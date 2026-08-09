using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.Options
{
    internal enum OptionsCategoryId
    {
        Video,
        Audio,
        Gameplay,
        Skin,
        Input,
        Miscellaneous,
        Advanced
    }

    internal enum OptionsIconFrame
    {
        Search,
        Video,
        Audio,
        Gameplay,
        Skin,
        Input,
        Miscellaneous,
        Advanced,
        Collapse,
        Expand
    }

    internal sealed class OptionsCategoryDefinition
    {
        internal OptionsCategoryId Id { get; }

        internal string LocalizationKey { get; }

        internal OptionsIconFrame Icon { get; }

        internal IReadOnlyList<string> SubcategoryLocalizationKeys { get; }

        internal OptionsCategoryDefinition(OptionsCategoryId id, string localizationKey,
            OptionsIconFrame icon, params string[] subcategoryLocalizationKeys)
        {
            Id = id;
            LocalizationKey = localizationKey;
            Icon = icon;
            SubcategoryLocalizationKeys = subcategoryLocalizationKeys;
        }
    }

    internal static class OptionsNavigationCatalog
    {
        internal const string AllLocalizationKey = "Screen_Selection_All";

        internal static IReadOnlyList<OptionsCategoryDefinition> Categories { get; } =
            Array.AsReadOnly(new[]
            {
                new OptionsCategoryDefinition(OptionsCategoryId.Video, "Screen_Options_Video",
                    OptionsIconFrame.Video,
                    "Screen_Options_Window", "Screen_Options_FrameRate"),
                new OptionsCategoryDefinition(OptionsCategoryId.Audio, "Screen_Options_Audio",
                    OptionsIconFrame.Audio,
                    "Screen_Options_Output", "Screen_Options_Volume", "Screen_Options_Offset",
                    "Screen_Options_Effects"),
                new OptionsCategoryDefinition(OptionsCategoryId.Gameplay, "Screen_Options_Gameplay",
                    OptionsIconFrame.Gameplay,
                    "Screen_Options_Background", "Screen_Options_Visuals", "Screen_Options_Sound",
                    "Screen_Options_Input", "Screen_Options_UserInterface",
                    "Screen_Options_Scoreboard", "Screen_Options_ProgressBar",
                    "Screen_Options_LaneCover"),
                new OptionsCategoryDefinition(OptionsCategoryId.Skin, "Screen_Options_Skin",
                    OptionsIconFrame.Skin,
                    "Screen_Options_Selection", "Screen_Options_Navigation",
                    "Screen_Options_Sharing", "Screen_Options_Configuration"),
                new OptionsCategoryDefinition(OptionsCategoryId.Input, "Screen_Options_Input",
                    OptionsIconFrame.Input,
                    "Screen_Options_GameplayControls", "Screen_Options_GameplayUserInterface",
                    "Screen_Options_UserInterface", "Screen_Options_SongSelection",
                    "Screen_Options_Misc"),
                new OptionsCategoryDefinition(OptionsCategoryId.Miscellaneous,
                    "Screen_Options_Miscellaneous", OptionsIconFrame.Miscellaneous,
                    "Screen_Options_NavigationMaintenance", "Screen_Options_InstalledGames",
                    "Screen_Options_Notifications", "Screen_Options_SongSelect"),
                new OptionsCategoryDefinition(OptionsCategoryId.Advanced, "Screen_Options_Advanced",
                    OptionsIconFrame.Advanced,
                    "Screen_Options_Video", "Screen_Options_Audio", "Screen_Options_Gameplay",
                    "Screen_Options_Skin", "Screen_Options_Input",
                    "Screen_Options_Miscellaneous")
            });
    }

    internal static class OptionsIconAtlas
    {
        private const int FrameSize = 60;

        private const int FrameStride = 84;

        internal const int MinimumWidth = FrameSize;

        internal const int MinimumHeight = FrameStride * 9 + FrameSize;

        internal static bool IsValid(Texture2D texture) => texture != null && !texture.IsDisposed &&
                                                            texture.Width >= MinimumWidth &&
                                                            texture.Height >= MinimumHeight;

        internal static TextureRegion GetRegion(Texture2D texture, OptionsIconFrame frame)
        {
            if (!IsValid(texture))
                throw new ArgumentException("The Options V2 icon atlas has invalid dimensions.", nameof(texture));

            return new TextureRegion(texture,
                new Rectangle(0, (int) frame * FrameStride, FrameSize, FrameSize));
        }
    }

    internal sealed class OptionsCategoryButton : RoundedButton
    {
        private SkinV2OptionsCategoryNavigationConfig Config { get; }

        private MarqueeSpriteText Marquee { get; }

        private Color IdleColor { get; }

        private Color HoverColor { get; }

        private Color SelectedColor { get; }

        private Color ForegroundColor { get; }

        private Color SelectedForegroundColor { get; }

        private bool Selected { get; set; }

        private float LabelExpansionProgress { get; set; }

        internal OptionsCategoryDefinition Definition { get; }

        internal OptionsCategoryButton(OptionsCategoryDefinition definition, TextureRegion icon,
            WobbleFontStore font, SkinV2OptionsCategoryNavigationConfig config,
            EventHandler clickAction) : base(clickAction)
        {
            Definition = definition;
            Config = config;
            IdleColor = SkinV2Color.Parse(config.RailButtonColor);
            HoverColor = SkinV2Color.Parse(config.RailButtonHoverColor);
            SelectedColor = SkinV2Color.Parse(config.RailButtonSelectedColor);
            ForegroundColor = SkinV2Color.Parse(config.ForegroundColor);
            SelectedForegroundColor = SkinV2Color.Parse(config.SelectedForegroundColor);
            Size = new ScalableVector2(config.ButtonHeight, config.ButtonHeight);
            CornerRadius = config.CornerRadius;
            PerformHoverFade = false;
            SetIcon(icon, new Vector2(config.IconSize, config.IconSize));
            Marquee = new MarqueeSpriteText(font,
                LocalizationManager.Get(definition.LocalizationKey), config.FontSize, 1)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                UsePreviousSpriteBatchOptions = true,
                Visible = false
            };
            ApplyContentLayout();
            ApplyColors();
        }

        public override void Update(GameTime gameTime)
        {
            Marquee.IsActive = LabelExpansionProgress > 0.001f && (IsHovered || Selected);
            base.Update(gameTime);
            ApplyContentLayout();
            ApplyColors();
        }

        internal void SetSelected(bool selected)
        {
            Selected = selected;
            Marquee.IsActive = LabelExpansionProgress > 0.001f && (IsHovered || Selected);
            ApplyColors();
        }

        internal void SetLabelExpansionProgress(float progress)
        {
            progress = MathHelper.Clamp(progress, 0, 1);
            LabelExpansionProgress = progress;
            Marquee.TextSprite.Alpha = progress;
            Marquee.Visible = progress > 0.001f;
            Marquee.IsActive = Marquee.Visible && (IsHovered || Selected);
        }

        private void ApplyContentLayout()
        {
            if (Icon == null || Marquee == null)
                return;

            Icon.Alignment = Alignment.MidLeft;
            Icon.X = Math.Max(0, (Config.ButtonHeight - Config.IconSize) / 2f);
            Marquee.Alignment = Alignment.MidLeft;
            Marquee.X = Icon.X + Config.IconSize + Config.LabelSpacing;
            Marquee.Size = new ScalableVector2(
                Math.Max(1, Width - Marquee.X - Config.HorizontalPadding), Height);
        }

        private void ApplyColors()
        {
            var foreground = Selected ? SelectedForegroundColor : ForegroundColor;
            Tint = Selected ? SelectedColor : IsHovered ? HoverColor : IdleColor;
            if (Icon != null)
                Icon.Tint = foreground;
            if (Marquee?.TextSprite != null)
                Marquee.TextSprite.Tint = foreground;
        }
    }

    internal sealed class OptionsSubcategoryButton : RoundedButton
    {
        private SkinV2OptionsCategoryNavigationConfig Config { get; }

        private MarqueeSpriteText Marquee { get; }

        private Color IdleColor { get; }

        private Color HoverColor { get; }

        private Color SelectedColor { get; }

        private Color ForegroundColor { get; }

        private Color SelectedForegroundColor { get; }

        private bool Selected { get; set; }

        internal string LocalizationKey { get; }

        internal OptionsSubcategoryButton(string localizationKey, WobbleFontStore font,
            SkinV2OptionsCategoryNavigationConfig config, EventHandler clickAction)
            : base(clickAction)
        {
            LocalizationKey = localizationKey;
            Config = config;
            IdleColor = SkinV2Color.Parse(config.SubcategoryButtonColor);
            HoverColor = SkinV2Color.Parse(config.SubcategoryButtonHoverColor);
            SelectedColor = SkinV2Color.Parse(config.SubcategoryButtonSelectedColor);
            ForegroundColor = SkinV2Color.Parse(config.ForegroundColor);
            SelectedForegroundColor = SkinV2Color.Parse(config.SelectedForegroundColor);
            Size = new ScalableVector2(config.ButtonHeight, config.ButtonHeight);
            CornerRadius = config.CornerRadius;
            PerformHoverFade = false;
            Marquee = new MarqueeSpriteText(font, LocalizationManager.Get(localizationKey),
                config.FontSize, 1)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                UsePreviousSpriteBatchOptions = true
            };
            ApplyContentLayout();
            ApplyColors();
        }

        public override void Update(GameTime gameTime)
        {
            Marquee.IsActive = IsHovered || Selected;
            base.Update(gameTime);
            ApplyContentLayout();
            ApplyColors();
        }

        internal void SetSelected(bool selected)
        {
            Selected = selected;
            Marquee.IsActive = IsHovered || Selected;
            ApplyColors();
        }

        private void ApplyContentLayout()
        {
            if (Marquee == null)
                return;

            Marquee.Alignment = Alignment.MidLeft;
            Marquee.X = Config.HorizontalPadding;
            Marquee.Size = new ScalableVector2(
                Math.Max(1, Width - Config.HorizontalPadding * 2), Height);
        }

        private void ApplyColors()
        {
            var foreground = Selected ? SelectedForegroundColor : ForegroundColor;
            Tint = Selected ? SelectedColor : IsHovered ? HoverColor : IdleColor;
            if (Marquee?.TextSprite != null)
                Marquee.TextSprite.Tint = foreground;
        }
    }
}
