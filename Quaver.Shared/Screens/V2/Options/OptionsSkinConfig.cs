using System.ComponentModel.DataAnnotations;
using Quaver.Shared.Skinning.V2;
using Wobble.Configuration;

namespace Quaver.Shared.Screens.V2.Options
{
    /// <summary>
    ///     Skin configuration owned by the Options V2 dialog.
    /// </summary>
    public sealed class SkinV2OptionsConfig
    {
        [Required]
        public SkinV2OptionsLayoutConfig Layout { get; set; } = new SkinV2OptionsLayoutConfig();

        [Required]
        public SkinV2OptionsBackdropConfig Backdrop { get; set; } = new SkinV2OptionsBackdropConfig();

        [Required]
        public SkinV2OptionsHeaderConfig Header { get; set; } = new SkinV2OptionsHeaderConfig();

        [Required]
        public SkinV2OptionsPanelConfig Panels { get; set; } = new SkinV2OptionsPanelConfig();

        [Required]
        public SkinV2OptionsRailConfig Rail { get; set; } = new SkinV2OptionsRailConfig();

        [Required]
        public SkinV2OptionsCategoryNavigationConfig Categories { get; set; } =
            new SkinV2OptionsCategoryNavigationConfig();

        [Required]
        public SkinV2OptionsSearchConfig Search { get; set; } = new SkinV2OptionsSearchConfig();

        [Required]
        public SkinV2OptionsPresetConfig Preset { get; set; } = new SkinV2OptionsPresetConfig();
    }

    public sealed class SkinV2OptionsLayoutConfig
    {
        [Range(0, 2048)]
        public float DialogInset { get; set; } = SkinV2Spacing.Spacing3Xl;

        [Range(1, 8192)]
        public float MaximumDialogWidth { get; set; } = 1268;

        [Range(1, 8192)]
        public float MaximumDialogHeight { get; set; } = 610;

        [Range(1, 8192)]
        public float HeaderHeight { get; set; } = 40;

        [Range(0, 2048)]
        public float PanelGap { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(1, 8192)]
        public float LeftRegionWidth { get; set; } = 318;

        [Range(1, 8192)]
        public float CompactLeftRegionWidth { get; set; } = 220;

        [Range(1, 8192)]
        public float CollapsedRailWidth { get; set; } = 60;

        [Range(1, 8192)]
        public float PresetWidth { get; set; } = 204;

        [Range(1, 8192)]
        public float TitleLabelWidth { get; set; } = 100;

        [Range(1, 8192)]
        public float CompactTitleWidth { get; set; } = 90;

        [Range(1, 8192)]
        public float CompactBreakpoint { get; set; } = 900;

        [Range(1, 8192)]
        public float MinimumSearchWidth { get; set; } = 120;
    }

    public sealed class SkinV2OptionsBackdropConfig
    {
        [ConfigEditable]
        [SkinColor]
        public string Color { get; set; } = "#000000FF";

        [Range(0d, 1d)]
        public float Opacity { get; set; } = 0.75f;

        [ConfigEditable]
        [SkinColor]
        public string GapColor { get; set; } = "#000000FF";

        [Range(0, 4096)]
        public float CornerRadius { get; set; } = SkinV2BorderRadiusConfig.Normal;
    }

    public sealed class SkinV2OptionsHeaderConfig
    {
        [ConfigEditable]
        [SkinColor]
        public string BackgroundColor { get; set; } = "#555555FF";

        [ConfigEditable]
        [SkinColor]
        public string ActiveTitleColor { get; set; } = "#A7A7A7FF";

        [ConfigEditable]
        [SkinColor]
        public string TextColor { get; set; } = "#FFFFFFFF";

        [ConfigEditable]
        [SkinColor]
        public string MutedTextColor { get; set; } = "#8A8A8AFF";

        [SkinFont]
        public string Font { get; set; } = SkinV2FontWeightsConfig.SemiBold;

        [Range(1, 256)]
        public int FontSize { get; set; } = SkinV2FontSizesConfig.TextLg;

        [Range(0, 2048)]
        public float HorizontalPadding { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(0, 4096)]
        public float CornerRadius { get; set; } = SkinV2BorderRadiusConfig.Normal;
    }

    public sealed class SkinV2OptionsPanelConfig
    {
        [ConfigEditable]
        [SkinColor]
        public string RailColor { get; set; } = "#555555FF";

        [ConfigEditable]
        [SkinColor]
        public string CategoryColor { get; set; } = "#898989FF";

        [ConfigEditable]
        [SkinColor]
        public string ContentColor { get; set; } = "#898989FF";

        [Range(0, 4096)]
        public float CornerRadius { get; set; } = SkinV2BorderRadiusConfig.Normal;
    }

    public sealed class SkinV2OptionsRailConfig
    {
        [Range(1, 10000)]
        public int ExpansionDurationMilliseconds { get; set; } = 180;

        [Range(1, 8192)]
        public float ToggleButtonSize { get; set; } = 40;

        [Range(1, 8192)]
        public float ToggleIconSize { get; set; } = SkinV2Spacing.Spacing3Xl;

        [Range(0, 2048)]
        public float ToggleInset { get; set; } = SkinV2Spacing.Spacing2Xs;

        [ConfigEditable]
        [SkinColor]
        public string ToggleColor { get; set; } = "#555555FF";

        [SkinColor]
        public string ToggleIconColor { get; set; } = "#FFFFFFFF";

        [Range(0, 4096)]
        public float ToggleCornerRadius { get; set; } = SkinV2BorderRadiusConfig.Normal;
    }

    public sealed class SkinV2OptionsCategoryNavigationConfig
    {
        [SkinAssetPath]
        public string IconAtlas { get; set; } = "";

        [SkinFont]
        public string Font { get; set; } = SkinV2FontWeightsConfig.SemiBold;

        [Range(1, 256)]
        public int FontSize { get; set; } = SkinV2FontSizesConfig.TextLg;

        [Range(1, 8192)]
        public float ButtonHeight { get; set; } = 40;

        [Range(1, 8192)]
        public float IconSize { get; set; } = 30;

        [Range(0, 2048)]
        public float PanelInset { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(0, 2048)]
        public float RowSpacing { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(0, 2048)]
        public float LabelSpacing { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(0, 2048)]
        public float HorizontalPadding { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(0, 4096)]
        public float CornerRadius { get; set; } = SkinV2BorderRadiusConfig.Normal;

        [Range(0, 128)]
        public float ScrollbarWidth { get; set; } = 3;

        [Range(0.01d, 1d)]
        public float LabelRevealProgress { get; set; } = 1;

        [ConfigEditable]
        [SkinColor]
        public string ForegroundColor { get; set; } = "#FFFFFFFF";

        [ConfigEditable]
        [SkinColor]
        public string SelectedForegroundColor { get; set; } = "#FFFFFFFF";

        [ConfigEditable]
        [SkinColor]
        public string RailButtonColor { get; set; } = "#00000000";

        [ConfigEditable]
        [SkinColor]
        public string RailButtonHoverColor { get; set; } = "#737373FF";

        [ConfigEditable]
        [SkinColor]
        public string RailButtonSelectedColor { get; set; } = "#A7A7A7FF";

        [ConfigEditable]
        [SkinColor]
        public string SubcategoryButtonColor { get; set; } = "#555555FF";

        [ConfigEditable]
        [SkinColor]
        public string SubcategoryButtonHoverColor { get; set; } = "#737373FF";

        [ConfigEditable]
        [SkinColor]
        public string SubcategoryButtonSelectedColor { get; set; } = "#A7A7A7FF";

        [ConfigEditable]
        [SkinColor]
        public string ScrollbarColor { get; set; } = "#A7A7A7FF";
    }

    public sealed class SkinV2OptionsSearchConfig
    {
        [ConfigEditable]
        [SkinColor]
        public string BackgroundColor { get; set; } = "#555555FF";

        [ConfigEditable]
        [SkinColor]
        public string TextColor { get; set; } = "#FFFFFFFF";

        [ConfigEditable]
        [SkinColor]
        public string PlaceholderColor { get; set; } = "#B8B8B8FF";

        [SkinColor]
        public string CursorColor { get; set; } = "#FFFFFFFF";

        [SkinColor]
        public string IconColor { get; set; } = "#B8B8B8FF";

        [SkinFont]
        public string Font { get; set; } = SkinV2FontWeightsConfig.SemiBold;

        [Range(1, 256)]
        public int FontSize { get; set; } = SkinV2FontSizesConfig.TextLg;

        [Range(1, 8192)]
        public float IconSize { get; set; } = SkinV2Spacing.Spacing3Xl;

        [Range(0, 2048)]
        public float HorizontalPadding { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(0, 2048)]
        public float TextLeftInset { get; set; } = 44;

        [Range(0, 2048)]
        public float ResultRightInset { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(1, 8192)]
        public float ResultWidth { get; set; } = 180;

        [Range(0, 4096)]
        public float CornerRadius { get; set; } = SkinV2BorderRadiusConfig.Normal;
    }

    public sealed class SkinV2OptionsPresetConfig
    {
        [ConfigEditable]
        [SkinColor]
        public string BackgroundColor { get; set; } = "#555555FF";

        [ConfigEditable]
        [SkinColor]
        public string TextColor { get; set; } = "#FFFFFFFF";

        [ConfigEditable]
        [SkinColor]
        public string MenuColor { get; set; } = "#454545FF";

        [ConfigEditable]
        [SkinColor]
        public string ItemColor { get; set; } = "#555555FF";

        [ConfigEditable]
        [SkinColor]
        public string SelectedItemColor { get; set; } = "#737373FF";

        [SkinFont]
        public string Font { get; set; } = SkinV2FontWeightsConfig.SemiBold;

        [Range(1, 256)]
        public int FontSize { get; set; } = SkinV2FontSizesConfig.TextLg;

        [Range(1, 8192)]
        public float IconSize { get; set; } = SkinV2Spacing.SpacingBase;

        [Range(0, 2048)]
        public float HorizontalPadding { get; set; } = SkinV2Spacing.Spacing2Xs;

        [Range(0, 2048)]
        public float MenuGap { get; set; } = SkinV2MarginsConfig.Sm;

        [Range(0, 2048)]
        public float MenuPadding { get; set; } = SkinV2MarginsConfig.Sm;

        [Range(0, 2048)]
        public float ItemSpacing { get; set; } = 2;

        [Range(1, 8192)]
        public float ItemHeight { get; set; } = 36;

        [Range(0, 4096)]
        public float CornerRadius { get; set; } = SkinV2BorderRadiusConfig.Normal;
    }
}
