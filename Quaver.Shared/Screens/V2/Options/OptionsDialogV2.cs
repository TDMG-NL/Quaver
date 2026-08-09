using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Input.Global;
using Quaver.Shared.Screens.V2.SkinEditor;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Graphics.UI.Form;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Screens.V2.Options
{
    /// <summary>
    ///     Options V2 modal shell with category navigation. Option controls are intentionally absent.
    /// </summary>
    internal sealed class OptionsDialogV2 : DialogScreen, ISkinV2EditorHost
    {
        private sealed class Token : GlobalInputScopeToken
        {
            private OptionsDialogV2 Dialog { get; }

            internal Token(OptionsDialogV2 dialog) => Dialog = dialog;

            public override GlobalInputScope Scope => GlobalInputScope.Options;

            public override GlobalInputHandleResult Handle(GlobalKeybindActions action,
                bool isKeyPress = true, bool isRelease = false) =>
                Dialog.HandleGlobalInputAction(action, isKeyPress, isRelease);
        }

        private SkinStoreV2Lease Skin { get; }

        private SkinV2Config RootConfig { get; set; }

        private SkinV2OptionsConfig Config => RootConfig.Screens.Options;

        private GlobalInputScopeToken GlobalInputToken { get; }

        private SkinEditorController SkinEditor { get; set; }

        public Container PreviewRoot { get; }

        public Container EditorRoot { get; }

        public string EditorGroupLabel => LocalizationManager.Get("Screen_Main_Options");

        public IReadOnlyList<SkinEditorTarget> EditorTargets { get; private set; } =
            Array.Empty<SkinEditorTarget>();

        private RoundedPanel RootSurface { get; set; }

        private FlexContainer Header { get; set; }

        private SplitRoundedPanel TitleGroup { get; set; }

        private MarqueeSpriteText ActiveTitleText { get; set; }

        private MarqueeSpriteText SubtitleText { get; set; }

        private RoundedPanel SearchPanel { get; set; }

        private OptionsSearchTextbox SearchBox { get; set; }

        private SpriteTextPlus SearchResultText { get; set; }

        private OptionsPresetDropdown PresetDropdown { get; set; }

        private FlexContainer Body { get; set; }

        private FlexContainer LeftRegion { get; set; }

        private Container RailSpacer { get; set; }

        private RoundedPanel CategoryPanel { get; set; }

        private RoundedPanel ContentPanel { get; set; }

        private RoundedPanel RailOverlay { get; set; }

        private RoundedButton RailToggle { get; set; }

        private Texture2D OptionsIcons { get; set; }

        private ScrollContainer CategoryNavigationScroll { get; set; }

        private FlexContainer CategoryNavigationList { get; set; }

        private ScrollContainer SubcategoryNavigationScroll { get; set; }

        private FlexContainer SubcategoryNavigationList { get; set; }

        private List<OptionsCategoryButton> CategoryButtons { get; } =
            new List<OptionsCategoryButton>();

        private List<OptionsSubcategoryButton> SubcategoryButtons { get; } =
            new List<OptionsSubcategoryButton>();

        private OptionsCategoryDefinition SelectedCategory { get; set; } =
            OptionsNavigationCatalog.Categories[0];

        private string SelectedSubcategoryKey { get; set; } =
            OptionsNavigationCatalog.AllLocalizationKey;

        private FlexItemOptions HeaderTitleOptions { get; set; }

        private FlexItemOptions HeaderSearchOptions { get; set; }

        private FlexItemOptions HeaderPresetOptions { get; set; }

        private FlexItemOptions LeftRegionOptions { get; set; }

        private FlexItemOptions RailSpacerOptions { get; set; }

        private bool RailExpanded { get; set; }

        private bool Closing { get; set; }

        private bool Destroyed { get; set; }

        private bool EditorLayoutActive { get; set; }

        private float EditorLeftWidth { get; set; }

        private float EditorRightWidth { get; set; }

        private float EditorBottomHeight { get; set; }

        private float LastWindowWidth { get; set; } = -1;

        private float LastWindowHeight { get; set; } = -1;

        private float CurrentLeftRegionWidth { get; set; }

        private float LastNavigationRailWidth { get; set; } = -1;

        private float LastNavigationBodyHeight { get; set; } = -1;

        private float LastNavigationCategoryWidth { get; set; } = -1;

        internal OptionsDialogV2() : base(0)
        {
            Skin = SkinManager.AcquireV2();
            RootConfig = Skin.Config;
            GlobalInputToken = new Token(this);
            Tint = SkinV2Color.Parse(Config.Backdrop.Color);
            Alpha = Config.Backdrop.Opacity;

            PreviewRoot = new Container
            {
                Parent = Container,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height),
                Pivot = Vector2.Zero
            };
            CreateContent();
            EditorRoot = new Container
            {
                Parent = Container,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height),
                Visible = false
            };

            Clicked += OnBackdropClicked;
            WindowManager.VirtualScreenSizeChanged += OnVirtualScreenSizeChanged;
        }

        public override void CreateContent()
        {
            var font = FontManager.GetWobbleFont(Config.Header.Font);
            OptionsIcons = LoadOptionsIconAtlas();

            RootSurface = new RoundedPanel(Config.Backdrop.CornerRadius)
            {
                Parent = PreviewRoot,
                Tint = SkinV2Color.Parse(Config.Backdrop.GapColor)
            };

            CreateBody();
            CreateRailOverlay();
            CreateCategoryNavigation();
            // Create the header last so its transient dropdown content draws above the body and rail.
            CreateHeader(font);
            EditorTargets = new[]
            {
                new SkinEditorTarget("options-dialog", LocalizationManager.Get("Screen_Main_Options"),
                    "Screens.Options", RootSurface)
            };
            UpdateResponsiveLayout(true);
        }

        public override void HandleInput(GameTime gameTime)
        {
            if (!KeyboardManager.IsCtrlDown() || !IsShiftDown() ||
                !KeyboardManager.IsUniqueKeyPress(Microsoft.Xna.Framework.Input.Keys.E))
                return;

            SkinEditor ??= new SkinEditorController(this);
            if (SkinEditor.IsOpen)
                SkinEditor.RequestClose();
            else
                SkinEditor.Open();
        }

        public override void Update(GameTime gameTime)
        {
            UpdateResponsiveLayout();
            ActiveTitleText.IsActive = ActiveTitleText.IsHovered();
            SubtitleText.IsActive = SubtitleText.Visible && SubtitleText.IsHovered();

            if (RailExpanded && MouseManager.IsUniqueClick(MouseButton.Left) &&
                RootSurface.IsHovered() && !RailOverlay.IsHovered())
                SetRailExpanded(false, true);

            if (RailExpanded && RailOverlay.Animations.Count == 0)
                CategoryPanel.Visible = false;

            UpdateNavigationLayout();
            CategoryNavigationScroll.InputEnabled = RailOverlay.Visible && RailOverlay.IsHovered();
            SubcategoryNavigationScroll.InputEnabled = CategoryPanel.Visible && CategoryPanel.IsHovered();

            base.Update(gameTime);
            UpdateCategoryLabelProgress();
        }

        public override void Destroy()
        {
            if (Destroyed)
                return;

            Destroyed = true;
            WindowManager.VirtualScreenSizeChanged -= OnVirtualScreenSizeChanged;
            SkinEditor?.Destroy();
            SkinEditor = null;
            GlobalInputToken.Dispose();
            OptionsDrawableCleanup.DestroyTree(Container);
            Skin.Dispose();
            base.Destroy();
        }

        public void SetSkinEditorLayout(bool active, float leftPanelWidth = 0,
            float rightPanelWidth = 0, float assetPanelHeight = 0)
        {
            EditorLayoutActive = active;
            EditorLeftWidth = leftPanelWidth;
            EditorRightWidth = rightPanelWidth;
            EditorBottomHeight = assetPanelHeight;
            EditorRoot.Visible = active;
            UpdateEditorLayout();
        }

        public void ApplySkinEditorPreview(SkinV2Config config)
        {
            RootConfig = config;
            Tint = SkinV2Color.Parse(Config.Backdrop.Color);
            Alpha = Config.Backdrop.Opacity;
            RailExpanded = false;
            CategoryButtons.Clear();
            SubcategoryButtons.Clear();

            foreach (var child in PreviewRoot.Children.ToArray())
                OptionsDrawableCleanup.DestroyTree(child);

            LastWindowWidth = -1;
            LastWindowHeight = -1;
            CreateContent();
            UpdateEditorLayout();
        }

        public void EnsureNavigation()
        {
        }

        private void CreateHeader(WobbleFontStore font)
        {
            Header = new FlexContainer
            {
                Parent = RootSurface,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                ColumnGap = Config.Layout.PanelGap
            };

            TitleGroup = new SplitRoundedPanel(Config.Header.CornerRadius,
                SkinV2Color.Parse(Config.Header.ActiveTitleColor),
                SkinV2Color.Parse(Config.Header.BackgroundColor))
            {
                Parent = Header
            };
            HeaderTitleOptions = new FlexItemOptions { Basis = Config.Layout.LeftRegionWidth, Shrink = 0 };
            Header.SetItemOptions(TitleGroup, HeaderTitleOptions);

            ActiveTitleText = CreateLabel(TitleGroup, font, LocalizationManager.Get("Screen_Main_Options"),
                Config.Header.TextColor);
            SubtitleText = CreateLabel(TitleGroup, font,
                LocalizationManager.Get("Screen_Options_AdjustGameSettings"), Config.Header.TextColor);

            SearchPanel = new RoundedPanel(Config.Search.CornerRadius)
            {
                Parent = Header,
                Tint = SkinV2Color.Parse(Config.Search.BackgroundColor)
            };
            HeaderSearchOptions = new FlexItemOptions
            {
                Basis = Config.Layout.MinimumSearchWidth,
                Grow = 1,
                Shrink = 1
            };
            Header.SetItemOptions(SearchPanel, HeaderSearchOptions);

            SearchBox = new OptionsSearchTextbox(font, Config.Search,
                OptionsIconAtlas.GetRegion(OptionsIcons, OptionsIconFrame.Search))
            {
                Parent = SearchPanel,
                Alignment = Alignment.MidLeft
            };
            SearchResultText = new SpriteTextPlus(font,
                LocalizationManager.Get("Screen_Options_OptionsFound", 0), Config.Search.FontSize)
            {
                Parent = SearchPanel,
                Alignment = Alignment.MidRight,
                X = -Config.Search.ResultRightInset,
                Tint = SkinV2Color.Parse(Config.Header.MutedTextColor),
                UsePreviousSpriteBatchOptions = true
            };

            PresetDropdown = new OptionsPresetDropdown(Config.Layout.PresetWidth,
                Config.Layout.HeaderHeight, Config.Preset)
            {
                Parent = Header
            };
            HeaderPresetOptions = new FlexItemOptions { Basis = Config.Layout.PresetWidth, Shrink = 0 };
            Header.SetItemOptions(PresetDropdown, HeaderPresetOptions);
        }

        private void CreateBody()
        {
            Body = new FlexContainer
            {
                Parent = RootSurface,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                ColumnGap = Config.Layout.PanelGap
            };

            LeftRegion = new FlexContainer
            {
                Parent = Body,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                ColumnGap = Config.Layout.PanelGap
            };
            LeftRegionOptions = new FlexItemOptions { Basis = Config.Layout.LeftRegionWidth, Shrink = 0 };
            Body.SetItemOptions(LeftRegion, LeftRegionOptions);

            RailSpacer = new Container { Parent = LeftRegion };
            RailSpacerOptions = new FlexItemOptions
            {
                Basis = Config.Layout.CollapsedRailWidth,
                Shrink = 0
            };
            LeftRegion.SetItemOptions(RailSpacer, RailSpacerOptions);

            CategoryPanel = new RoundedPanel(Config.Panels.CornerRadius)
            {
                Parent = LeftRegion,
                Tint = SkinV2Color.Parse(Config.Panels.CategoryColor)
            };
            LeftRegion.SetItemOptions(CategoryPanel,
                new FlexItemOptions { Basis = 1, Grow = 1, Shrink = 1 });

            ContentPanel = new RoundedPanel(Config.Panels.CornerRadius)
            {
                Parent = Body,
                Tint = SkinV2Color.Parse(Config.Panels.ContentColor)
            };
            Body.SetItemOptions(ContentPanel,
                new FlexItemOptions { Basis = 1, Grow = 1, Shrink = 1 });
        }

        private void CreateRailOverlay()
        {
            RailOverlay = new RoundedPanel(Config.Panels.CornerRadius)
            {
                Parent = RootSurface,
                Tint = SkinV2Color.Parse(Config.Panels.RailColor),
                DrawOrder = 100
            };

            RailToggle = new RoundedButton((sender, args) => SetRailExpanded(!RailExpanded, true))
            {
                Parent = RailOverlay,
                Alignment = Alignment.BotLeft,
                Position = new ScalableVector2(Config.Rail.ToggleInset, -Config.Rail.ToggleInset),
                Size = new ScalableVector2(Config.Rail.ToggleButtonSize, Config.Rail.ToggleButtonSize),
                CornerRadius = Config.Rail.ToggleCornerRadius,
                Tint = SkinV2Color.Parse(Config.Rail.ToggleColor),
                PerformHoverFade = true,
                Depth = -100
            };
            RailToggle.SetIcon(OptionsIconAtlas.GetRegion(OptionsIcons, OptionsIconFrame.Collapse),
                new Vector2(Config.Rail.ToggleIconSize, Config.Rail.ToggleIconSize));
            RailToggle.Icon.Tint = SkinV2Color.Parse(Config.Rail.ToggleIconColor);
            UpdateRailIcon();
        }

        private void CreateCategoryNavigation()
        {
            var config = Config.Categories;
            var font = FontManager.GetWobbleFont(config.Font);
            var categoryContentHeight = GetListContentHeight(OptionsNavigationCatalog.Categories.Count);

            CategoryNavigationScroll = CreateNavigationScroll(RailOverlay, categoryContentHeight);
            CategoryNavigationList = CreateNavigationList(categoryContentHeight);
            CategoryButtons.Clear();

            foreach (var definition in OptionsNavigationCatalog.Categories)
            {
                var button = new OptionsCategoryButton(definition,
                    OptionsIconAtlas.GetRegion(OptionsIcons, definition.Icon), font, config,
                    (sender, args) => SelectCategory(definition))
                {
                    Parent = CategoryNavigationList
                };
                CategoryNavigationList.SetItemOptions(button,
                    new FlexItemOptions { Basis = config.ButtonHeight, Shrink = 0 });
                CategoryButtons.Add(button);
            }

            CategoryNavigationScroll.AddContainedDrawable(CategoryNavigationList);
            ApplyCategorySelection();
            CreateSubcategoryNavigation();
        }

        private void CreateSubcategoryNavigation()
        {
            OptionsDrawableCleanup.DestroyTree(SubcategoryNavigationScroll);
            SubcategoryButtons.Clear();

            var config = Config.Categories;
            var font = FontManager.GetWobbleFont(config.Font);
            var keys = new[] { OptionsNavigationCatalog.AllLocalizationKey }
                .Concat(SelectedCategory.SubcategoryLocalizationKeys).ToArray();
            var contentHeight = GetListContentHeight(keys.Length);
            SubcategoryNavigationScroll = CreateNavigationScroll(CategoryPanel, contentHeight);
            SubcategoryNavigationList = CreateNavigationList(contentHeight);

            foreach (var key in keys)
            {
                var capturedKey = key;
                var button = new OptionsSubcategoryButton(capturedKey, font, config,
                    (sender, args) => SelectSubcategory(capturedKey))
                {
                    Parent = SubcategoryNavigationList
                };
                SubcategoryNavigationList.SetItemOptions(button,
                    new FlexItemOptions { Basis = config.ButtonHeight, Shrink = 0 });
                SubcategoryButtons.Add(button);
            }

            SubcategoryNavigationScroll.AddContainedDrawable(SubcategoryNavigationList);
            ApplySubcategorySelection();
            LastNavigationCategoryWidth = -1;
            UpdateNavigationLayout(true);
        }

        private ScrollContainer CreateNavigationScroll(Drawable parent, float contentHeight)
        {
            var config = Config.Categories;
            return new ScrollContainer(new ScalableVector2(1, 1),
                new ScalableVector2(1, Math.Max(1, contentHeight)))
            {
                Parent = parent,
                Position = new ScalableVector2(config.PanelInset, config.PanelInset),
                Tint = Color.Transparent,
                InputEnabled = true,
                AllowScrollbarDragging = true,
                ScrollSpeed = 80,
                Scrollbar =
                {
                    Width = config.ScrollbarWidth,
                    Tint = SkinV2Color.Parse(config.ScrollbarColor)
                }
            };
        }

        private FlexContainer CreateNavigationList(float contentHeight)
        {
            var list = new FlexContainer
            {
                Size = new ScalableVector2(1, Math.Max(1, contentHeight)),
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Config.Categories.RowSpacing
            };
            return list;
        }

        private float GetListContentHeight(int itemCount)
        {
            if (itemCount <= 0)
                return 1;

            return itemCount * Config.Categories.ButtonHeight +
                   Math.Max(0, itemCount - 1) * Config.Categories.RowSpacing;
        }

        private Texture2D LoadOptionsIconAtlas()
        {
            var fallback = UserInterface.OptionsV2Icons;
            if (!OptionsIconAtlas.IsValid(fallback))
            {
                throw new InvalidOperationException(
                    $"The bundled Options V2 icon atlas must be at least " +
                    $"{OptionsIconAtlas.MinimumWidth}x{OptionsIconAtlas.MinimumHeight} pixels.");
            }

            var texture = Skin.LoadTexture(Config.Categories.IconAtlas, fallback);
            if (OptionsIconAtlas.IsValid(texture))
                return texture;

            Logger.Warning($"The configured Options V2 icon atlas must be at least " +
                           $"{OptionsIconAtlas.MinimumWidth}x{OptionsIconAtlas.MinimumHeight} pixels; " +
                           "the bundled atlas will be used instead.", LogType.Runtime, false);
            return fallback;
        }

        private void SelectCategory(OptionsCategoryDefinition category)
        {
            SelectedCategory = category;
            SelectedSubcategoryKey = OptionsNavigationCatalog.AllLocalizationKey;
            ApplyCategorySelection();
            CreateSubcategoryNavigation();

            if (RailExpanded)
                SetRailExpanded(false, true);
        }

        private void SelectSubcategory(string localizationKey)
        {
            SelectedSubcategoryKey = localizationKey;
            ApplySubcategorySelection();
        }

        private void ApplyCategorySelection()
        {
            foreach (var button in CategoryButtons)
                button.SetSelected(button.Definition.Id == SelectedCategory.Id);
        }

        private void ApplySubcategorySelection()
        {
            foreach (var button in SubcategoryButtons)
            {
                button.SetSelected(string.Equals(button.LocalizationKey, SelectedSubcategoryKey,
                    StringComparison.Ordinal));
            }
        }

        private MarqueeSpriteText CreateLabel(Sprite parent, WobbleFontStore font, string text, string color)
        {
            var label = new MarqueeSpriteText(font, text, Config.Header.FontSize, 1)
            {
                Parent = parent,
                Alignment = Alignment.MidLeft
            };
            label.TextSprite.Tint = SkinV2Color.Parse(color);
            return label;
        }

        private void UpdateResponsiveLayout(bool force = false)
        {
            var windowWidth = WindowManager.Width;
            var windowHeight = WindowManager.Height;
            if (!force && Math.Abs(windowWidth - LastWindowWidth) < 0.001f &&
                Math.Abs(windowHeight - LastWindowHeight) < 0.001f)
                return;

            LastWindowWidth = windowWidth;
            LastWindowHeight = windowHeight;
            Container.Size = new ScalableVector2(windowWidth, windowHeight);
            PreviewRoot.Size = Container.Size;
            if (EditorRoot != null)
                EditorRoot.Size = Container.Size;

            var horizontalInset = Math.Min(Config.Layout.DialogInset, Math.Max(0, (windowWidth - 1) / 2f));
            var verticalInset = Math.Min(Config.Layout.DialogInset, Math.Max(0, (windowHeight - 1) / 2f));
            var rootWidth = Math.Min(Config.Layout.MaximumDialogWidth,
                Math.Max(1, windowWidth - horizontalInset * 2));
            var rootHeight = Math.Min(Config.Layout.MaximumDialogHeight,
                Math.Max(1, windowHeight - verticalInset * 2));
            var compact = windowWidth <= Config.Layout.CompactBreakpoint;
            var desiredLeftWidth = compact
                ? Config.Layout.CompactLeftRegionWidth
                : Config.Layout.LeftRegionWidth;
            var minimumLeftWidth = Config.Layout.CollapsedRailWidth + Config.Layout.PanelGap + 1;
            CurrentLeftRegionWidth = Math.Min(desiredLeftWidth,
                Math.Max(minimumLeftWidth, rootWidth - Config.Layout.PanelGap - 1));

            RootSurface.Position = new ScalableVector2((windowWidth - rootWidth) / 2f,
                (windowHeight - rootHeight) / 2f);
            RootSurface.Size = new ScalableVector2(rootWidth, rootHeight);

            Header.Position = new ScalableVector2(0, 0);
            Header.Size = new ScalableVector2(rootWidth,
                Math.Min(Config.Layout.HeaderHeight, rootHeight));

            var bodyY = Math.Min(rootHeight, Config.Layout.HeaderHeight + Config.Layout.PanelGap);
            Body.Position = new ScalableVector2(0, bodyY);
            Body.Size = new ScalableVector2(rootWidth, Math.Max(1, rootHeight - bodyY));

            HeaderTitleOptions.Basis = compact
                ? Math.Min(Config.Layout.CompactTitleWidth, CurrentLeftRegionWidth)
                : CurrentLeftRegionWidth;

            var availablePresetWidth = Math.Max(1,
                rootWidth - HeaderTitleOptions.Basis.Value - Config.Layout.PanelGap * 2 -
                Config.Layout.MinimumSearchWidth);
            HeaderPresetOptions.Basis = Math.Min(Config.Layout.PresetWidth, availablePresetWidth);
            LeftRegionOptions.Basis = CurrentLeftRegionWidth;
            RailSpacerOptions.Basis = Math.Min(Config.Layout.CollapsedRailWidth,
                CurrentLeftRegionWidth);

            Header.RefreshLayout();
            LayoutTitleGroup(compact);
            Body.RefreshLayout();
            LeftRegion.RefreshLayout();

            LayoutSearch();
            PresetDropdown.Size = new ScalableVector2(PresetDropdown.Width, Header.Height);

            RailOverlay.Position = new ScalableVector2(0, bodyY);
            RailOverlay.Animations.Clear();
            RailOverlay.Size = new ScalableVector2(
                RailExpanded ? CurrentLeftRegionWidth : RailSpacerOptions.Basis.Value,
                Body.Height);
            CategoryPanel.Visible = !RailExpanded;

            var toggleSize = Math.Min(Config.Rail.ToggleButtonSize,
                Math.Max(1, Body.Height - Config.Rail.ToggleInset * 2));
            RailToggle.Size = new ScalableVector2(toggleSize, toggleSize);
            PresetDropdown.CloseMenu();
            LastNavigationRailWidth = -1;
            LastNavigationBodyHeight = -1;
            LastNavigationCategoryWidth = -1;
            UpdateNavigationLayout(true);
            UpdateCategoryLabelProgress();
            UpdateEditorLayout();
        }

        private void UpdateNavigationLayout(bool force = false)
        {
            if (CategoryNavigationScroll == null || SubcategoryNavigationScroll == null ||
                RailOverlay == null || CategoryPanel == null || Body == null)
                return;

            if (!force && Math.Abs(LastNavigationRailWidth - RailOverlay.Width) < 0.001f &&
                Math.Abs(LastNavigationBodyHeight - Body.Height) < 0.001f &&
                Math.Abs(LastNavigationCategoryWidth - CategoryPanel.Width) < 0.001f)
                return;

            LastNavigationRailWidth = RailOverlay.Width;
            LastNavigationBodyHeight = Body.Height;
            LastNavigationCategoryWidth = CategoryPanel.Width;

            var config = Config.Categories;
            var inset = config.PanelInset;
            var railWidth = Math.Max(1, RailOverlay.Width - inset * 2);
            var toggleTop = Math.Max(inset,
                Body.Height - Config.Rail.ToggleInset - RailToggle.Height);
            var railHeight = Math.Max(1, toggleTop - inset * 2);
            LayoutNavigationScroll(CategoryNavigationScroll, CategoryNavigationList,
                railWidth, railHeight, GetListContentHeight(CategoryButtons.Count));

            var categoryWidth = Math.Max(1, CategoryPanel.Width - inset * 2);
            var categoryHeight = Math.Max(1, CategoryPanel.Height - inset * 2);
            LayoutNavigationScroll(SubcategoryNavigationScroll, SubcategoryNavigationList,
                categoryWidth, categoryHeight, GetListContentHeight(SubcategoryButtons.Count));
        }

        private void LayoutNavigationScroll(ScrollContainer scroll, FlexContainer list,
            float viewportWidth, float viewportHeight, float contentHeight)
        {
            scroll.Position = new ScalableVector2(Config.Categories.PanelInset,
                Config.Categories.PanelInset);
            scroll.Size = new ScalableVector2(viewportWidth, viewportHeight);
            scroll.ContentContainer.Size = new ScalableVector2(viewportWidth,
                Math.Max(viewportHeight, contentHeight));
            list.Size = new ScalableVector2(viewportWidth, Math.Max(1, contentHeight));
            list.RefreshLayout();
        }

        private void UpdateCategoryLabelProgress()
        {
            if (RailOverlay == null || CategoryButtons.Count == 0)
                return;

            var collapsedWidth = RailSpacerOptions?.Basis ?? Config.Layout.CollapsedRailWidth;
            var widthRange = Math.Max(0.001f, CurrentLeftRegionWidth - collapsedWidth);
            var progress = MathHelper.Clamp((RailOverlay.Width - collapsedWidth) / widthRange, 0, 1);
            var revealProgress = Math.Max(0.001f, Config.Categories.LabelRevealProgress);
            var labelProgress = MathHelper.SmoothStep(0, 1,
                MathHelper.Clamp(progress / revealProgress, 0, 1));

            foreach (var button in CategoryButtons)
                button.SetLabelExpansionProgress(labelProgress);
        }

        private void UpdateEditorLayout()
        {
            if (PreviewRoot == null || EditorRoot == null)
                return;

            Container.Size = new ScalableVector2(WindowManager.Width, WindowManager.Height);
            PreviewRoot.Size = Container.Size;
            EditorRoot.Size = Container.Size;

            if (!EditorLayoutActive)
            {
                PreviewRoot.Position = new ScalableVector2(0, 0);
                PreviewRoot.Scale = Vector2.One;
                return;
            }

            const float margin = 16;
            var availableWidth = Math.Max(1,
                WindowManager.Width - EditorLeftWidth - EditorRightWidth - margin * 2);
            var availableHeight = Math.Max(1,
                WindowManager.Height - EditorBottomHeight - margin * 2);
            var scale = Math.Min(availableWidth / WindowManager.Width,
                availableHeight / WindowManager.Height);
            PreviewRoot.Scale = new Vector2(scale);
            PreviewRoot.Position = new ScalableVector2(
                EditorLeftWidth + margin + (availableWidth - WindowManager.Width * scale) / 2f,
                margin + (availableHeight - WindowManager.Height * scale) / 2f);
        }

        private void LayoutSearch()
        {
            var resultWidth = Math.Min(Config.Search.ResultWidth,
                Math.Max(1, SearchPanel.Width - Config.Layout.MinimumSearchWidth));
            SearchBox.Size = new ScalableVector2(Math.Max(1, SearchPanel.Width - resultWidth),
                SearchPanel.Height);
            SearchResultText.X = -Config.Search.ResultRightInset;
        }

        private void LayoutTitleGroup(bool compact)
        {
            var activeWidth = compact
                ? TitleGroup.Width
                : Math.Min(Config.Layout.TitleLabelWidth, TitleGroup.Width);
            var subtitleWidth = Math.Max(0, TitleGroup.Width - activeWidth);
            var padding = Config.Header.HorizontalPadding;

            TitleGroup.SplitPosition = activeWidth;
            ActiveTitleText.Position = new ScalableVector2(padding, 0);
            ActiveTitleText.Size = new ScalableVector2(
                Math.Max(1, activeWidth - padding * 2), TitleGroup.Height);
            SubtitleText.Position = new ScalableVector2(activeWidth + padding, 0);
            SubtitleText.Size = new ScalableVector2(
                Math.Max(1, subtitleWidth - padding * 2), TitleGroup.Height);
            SubtitleText.Visible = !compact && subtitleWidth > 0;
        }

        private void SetRailExpanded(bool expanded, bool animate)
        {
            if (RailExpanded == expanded)
                return;

            RailExpanded = expanded;
            if (!expanded)
                CategoryPanel.Visible = true;
            RailOverlay.Animations.Clear();
            var targetWidth = expanded
                ? CurrentLeftRegionWidth
                : RailSpacerOptions.Basis ?? Config.Layout.CollapsedRailWidth;
            if (animate)
                RailOverlay.ChangeWidthTo((int) targetWidth, Easing.OutCubic,
                    Config.Rail.ExpansionDurationMilliseconds);
            else
            {
                RailOverlay.Width = targetWidth;
                CategoryPanel.Visible = !expanded;
            }
            UpdateCategoryLabelProgress();
            UpdateRailIcon();
        }

        private void UpdateRailIcon()
        {
            if (RailToggle == null || OptionsIcons == null)
                return;

            RailToggle.SetIcon(OptionsIconAtlas.GetRegion(OptionsIcons,
                    RailExpanded ? OptionsIconFrame.Collapse : OptionsIconFrame.Expand),
                new Vector2(Config.Rail.ToggleIconSize, Config.Rail.ToggleIconSize));
            RailToggle.Icon.Tint = SkinV2Color.Parse(Config.Rail.ToggleIconColor);
        }

        private GlobalInputHandleResult HandleGlobalInputAction(GlobalKeybindActions action,
            bool isKeyPress, bool isRelease)
        {
            if (!IsOnTop || !isKeyPress || isRelease ||
                action.BaseWithLayer() != GlobalKeybindActions.Back)
                return GlobalInputHandleResult.Pass;

            if (SkinEditor?.IsOpen == true)
                SkinEditor.RequestClose();
            else if (PresetDropdown.IsOpen)
                PresetDropdown.CloseMenu();
            else if (SearchBox.Focused)
                SearchBox.Focused = false;
            else if (RailExpanded)
                SetRailExpanded(false, true);
            else
                Close();

            return GlobalInputHandleResult.Consumed;
        }

        private void OnBackdropClicked(object? sender, EventArgs args)
        {
            if (SkinEditor?.IsOpen == true)
                return;

            if (!RootSurface.IsHovered())
                Close();
        }

        private void OnVirtualScreenSizeChanged(object? sender,
            WindowVirtualScreenSizeChangedEventArgs args) => UpdateResponsiveLayout(true);

        private void Close()
        {
            if (Closing)
                return;

            Closing = true;
            DialogManager.Dismiss(this);
        }

        private static bool IsShiftDown() =>
            KeyboardManager.CurrentState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
            KeyboardManager.CurrentState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
    }

    internal sealed class SplitRoundedPanel : Sprite
    {
        private float CornerRadius { get; }

        private Color LeftColor { get; }

        private Color RightColor { get; }

        private Texture2D OwnedTexture { get; set; }

        private bool RefreshingTexture { get; set; }

        private float _splitPosition;

        internal float SplitPosition
        {
            get => _splitPosition;
            set
            {
                if (Math.Abs(_splitPosition - value) < 0.001f)
                    return;

                _splitPosition = value;
                RefreshTexture();
            }
        }

        internal SplitRoundedPanel(float cornerRadius, Color leftColor, Color rightColor)
        {
            CornerRadius = cornerRadius;
            LeftColor = leftColor;
            RightColor = rightColor;
            Tint = Color.White;
        }

        protected override void OnRectangleRecalculated()
        {
            base.OnRectangleRecalculated();
            RefreshTexture();
        }

        public override void Destroy()
        {
            RefreshingTexture = true;
            try
            {
                Image = null;
                OwnedTexture?.Dispose();
                OwnedTexture = null;
            }
            finally
            {
                RefreshingTexture = false;
            }
            base.Destroy();
        }

        private void RefreshTexture()
        {
            if (RefreshingTexture || Width <= 0 || Height <= 0)
                return;

            RefreshingTexture = true;
            try
            {
                var textureWidth = Math.Max(1, (int) Math.Ceiling(Width));
                var textureHeight = Math.Max(1, (int) Math.Ceiling(Height));
                var radius = MathHelper.Clamp(CornerRadius, 0,
                    Math.Min(textureWidth, textureHeight) / 2f);
                var split = MathHelper.Clamp(SplitPosition * textureWidth / Width, 0, textureWidth);
                var halfWidth = textureWidth / 2f;
                var halfHeight = textureHeight / 2f;
                var pixels = new Color[textureWidth * textureHeight];

                for (var y = 0; y < textureHeight; y++)
                {
                    for (var x = 0; x < textureWidth; x++)
                    {
                        var qx = Math.Abs(x + 0.5f - halfWidth) - (halfWidth - radius);
                        var qy = Math.Abs(y + 0.5f - halfHeight) - (halfHeight - radius);
                        var outsideDistance = (float) Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) +
                                                                Math.Max(qy, 0) * Math.Max(qy, 0));
                        var distance = outsideDistance + Math.Min(Math.Max(qx, qy), 0) - radius;
                        var coverage = 1 - SmoothStep(-1, 0, distance);
                        var source = x + 0.5f < split ? LeftColor : RightColor;
                        pixels[y * textureWidth + x] = new Color(source.R, source.G, source.B,
                            (byte) Math.Round(source.A * coverage));
                    }
                }

                var texture = new Texture2D(GameBase.Game.GraphicsDevice, textureWidth, textureHeight,
                    false, SurfaceFormat.Color);
                texture.SetData(pixels);

                var previous = OwnedTexture;
                OwnedTexture = texture;
                Image = texture;
                previous?.Dispose();
            }
            finally
            {
                RefreshingTexture = false;
            }
        }

        private static float SmoothStep(float min, float max, float value)
        {
            var amount = MathHelper.Clamp((value - min) / (max - min), 0, 1);
            return amount * amount * (3 - 2 * amount);
        }
    }

    internal sealed class RoundedPanel : Sprite
    {
        private float CornerRadius { get; }

        internal RoundedPanel(float cornerRadius) => CornerRadius = cornerRadius;

        protected override void OnRectangleRecalculated()
        {
            base.OnRectangleRecalculated();
            if (Width <= 0 || Height <= 0)
                return;

            var texture = RoundedRectTextureCache.Get(Width, Height, CornerRadius);
            if (Image != texture)
                Image = texture;
        }
    }

    internal sealed class OptionsSearchTextbox : Textbox
    {
        private SkinV2OptionsSearchConfig Config { get; }

        private Color TextColor { get; }

        private Color PlaceholderColor { get; }

        private Sprite SearchIcon { get; }

        internal OptionsSearchTextbox(WobbleFontStore font, SkinV2OptionsSearchConfig config,
            TextureRegion searchIcon)
            : base(new ScalableVector2(1, 1), font, config.FontSize, "",
                LocalizationManager.Get("Screen_Options_Searchforoptions"))
        {
            Config = config;
            TextColor = SkinV2Color.Parse(config.TextColor);
            PlaceholderColor = SkinV2Color.Parse(config.PlaceholderColor);
            Image = null;
            Alpha = 0;
            Cursor.Tint = SkinV2Color.Parse(config.CursorColor);
            InputText.X = config.TextLeftInset;
            InputEnabled = false;
            Scrollbar.Visible = false;
            StoppedTypingActionCalltime = 100;

            SearchIcon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = config.HorizontalPadding,
                Region = searchIcon,
                Size = new ScalableVector2(config.IconSize, config.IconSize),
                Tint = SkinV2Color.Parse(config.IconColor),
                UsePreviousSpriteBatchOptions = true
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            InputText.Tint = string.IsNullOrEmpty(RawText) ? PlaceholderColor : TextColor;
            InputText.Alpha = 1;
        }

        protected override void OnRectangleRecalculated()
        {
            base.OnRectangleRecalculated();
            if (Config == null)
                return;

            Button.Size = Size;
            ContentContainer.Size = Size;
        }
    }

    internal sealed class OptionsPresetDropdown : Container
    {
        private SkinV2OptionsPresetConfig Config { get; }

        private WobbleFontStore Font { get; }

        private IReadOnlyList<QuaverPresetDescriptor> Presets { get; }

        private RoundedButton Trigger { get; }

        private Sprite Menu { get; set; }

        internal bool IsOpen => Menu != null;

        internal OptionsPresetDropdown(float width, float height, SkinV2OptionsPresetConfig config)
        {
            Config = config;
            Font = FontManager.GetWobbleFont(config.Font);
            Presets = QuaverYamlConfigManager.GetPresets();
            Size = new ScalableVector2(width, height);

            Trigger = new RoundedButton((sender, args) =>
            {
                if (IsOpen)
                    CloseMenu();
                else
                    OpenMenu();
            })
            {
                Parent = this,
                Size = Size,
                CornerRadius = config.CornerRadius,
                Tint = SkinV2Color.Parse(config.BackgroundColor),
                PerformHoverFade = true,
                Depth = -100
            };
            Trigger.SetIcon(FontAwesome.Get(FontAwesomeIcon.fa_chevron_arrow_down),
                new Vector2(config.IconSize, config.IconSize));
            Trigger.Icon.Tint = SkinV2Color.Parse(config.TextColor);
            Trigger.SetLabel(Font, GetActiveLabel(), config.FontSize,
                SkinV2Color.Parse(config.TextColor));
            LayoutTrigger();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            LayoutTrigger();

            if (Menu != null && MouseManager.IsUniqueClick(MouseButton.Left) &&
                !Contains(Trigger.ScreenRectangle, MouseManager.CurrentState.Position) &&
                !Contains(Menu.ScreenRectangle, MouseManager.CurrentState.Position))
                CloseMenu();
        }

        protected override void OnRectangleRecalculated()
        {
            base.OnRectangleRecalculated();
            if (Trigger == null)
                return;

            Trigger.Size = Size;
            LayoutTrigger();
        }

        internal void CloseMenu()
        {
            OptionsDrawableCleanup.DestroyTree(Menu);
            Menu = null;
        }

        private void OpenMenu()
        {
            var padding = Config.MenuPadding;
            Menu = new RoundedPanel(Config.CornerRadius)
            {
                Parent = this,
                Position = new ScalableVector2(0, Height + Config.MenuGap),
                Size = new ScalableVector2(Width,
                    padding * 2 + Presets.Count * Config.ItemHeight +
                    Math.Max(0, Presets.Count - 1) * Config.ItemSpacing),
                Tint = SkinV2Color.Parse(Config.MenuColor),
                DrawOrder = 200
            };

            for (var index = 0; index < Presets.Count; index++)
            {
                var preset = Presets[index];
                var selected = string.Equals(preset.Id, QuaverYamlConfigManager.ActivePresetId,
                    StringComparison.OrdinalIgnoreCase);
                var row = new RoundedButton((sender, args) => SelectPreset(preset))
                {
                    Parent = Menu,
                    Position = new ScalableVector2(padding,
                        padding + index * (Config.ItemHeight + Config.ItemSpacing)),
                    Size = new ScalableVector2(Width - padding * 2, Config.ItemHeight),
                    CornerRadius = Config.CornerRadius,
                    Tint = SkinV2Color.Parse(selected
                        ? Config.SelectedItemColor
                        : Config.ItemColor),
                    PerformHoverFade = true,
                    Depth = -200
                };
                row.SetLabel(Font, GetLabel(preset), Config.FontSize,
                    SkinV2Color.Parse(Config.TextColor));
            }
        }

        private void SelectPreset(QuaverPresetDescriptor preset)
        {
            if (!QuaverYamlConfigManager.TrySelectPreset(preset.Id, out var errors))
            {
                var reason = errors == null || errors.Count == 0
                    ? "Unknown error"
                    : string.Join("; ", errors);
                NotificationManager.Show(NotificationLevel.Error,
                    LocalizationManager.Get("Screen_Options_PresetSaveFailed", reason), forceShow: true);
                CloseMenu();
                return;
            }

            Trigger.SetLabel(Font, GetActiveLabel(), Config.FontSize,
                SkinV2Color.Parse(Config.TextColor));
            LayoutTrigger();
            CloseMenu();
        }

        private string GetActiveLabel()
        {
            var active = Presets.FirstOrDefault(x => string.Equals(x.Id,
                QuaverYamlConfigManager.ActivePresetId, StringComparison.OrdinalIgnoreCase));
            return active == null ? LocalizationManager.Get("Screen_Options_PresetGraphics") : GetLabel(active);
        }

        private static string GetLabel(QuaverPresetDescriptor preset) => preset.IsBuiltIn
            ? LocalizationManager.Get(preset.NameOrLocalizationKey)
            : preset.NameOrLocalizationKey;

        private void LayoutTrigger()
        {
            if (Trigger?.Label != null)
            {
                Trigger.Label.Alignment = Alignment.MidLeft;
                Trigger.Label.X = Config.HorizontalPadding;
            }

            if (Trigger?.Icon != null)
            {
                Trigger.Icon.Alignment = Alignment.MidRight;
                Trigger.Icon.X = -Config.HorizontalPadding;
            }
        }

        private static bool Contains(RectangleF rectangle, Vector2 point) =>
            point.X >= rectangle.Left && point.X <= rectangle.Right &&
            point.Y >= rectangle.Top && point.Y <= rectangle.Bottom;
    }

    internal static class OptionsDrawableCleanup
    {
        internal static void DestroyTree(Drawable root)
        {
            if (root == null)
                return;

            foreach (var child in root.Children.ToArray())
                DestroyTree(child);

            root.Destroy();
        }
    }
}
