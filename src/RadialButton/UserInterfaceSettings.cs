using System;
using System.Collections.Generic;
using System.Drawing;

namespace RadialButton
{
    public sealed class UserInterfaceSettings
    {
        public string SelectedColorScheme { get; set; }
        public double WheelScale { get; set; }
        public string KeyboardBackButton { get; set; }
        public string MouseBackButton { get; set; }
        public List<WheelColorScheme> ColorSchemes { get; set; }

        public UserInterfaceSettings()
        {
            ColorSchemes = new List<WheelColorScheme>();
        }

        public WheelColorScheme GetSelectedScheme()
        {
            WheelColorScheme selected = ColorSchemes.Find(value =>
                string.Equals(value.Name, SelectedColorScheme, StringComparison.OrdinalIgnoreCase));

            return selected ?? (ColorSchemes.Count > 0 ? ColorSchemes[0] : WheelColorScheme.CreateDefault());
        }
    }

    public sealed class WheelColorScheme
    {
        public string Name { get; set; }
        public string BackdropColor { get; set; }
        public string SectorColor { get; set; }
        public string SectorHoverColor { get; set; }
        public string SectorBorderColor { get; set; }
        public string SectorHoverBorderColor { get; set; }
        public string TabSectorColor { get; set; }
        public string TabSectorHoverColor { get; set; }
        public string TabSectorBorderColor { get; set; }
        public string TabSectorHoverBorderColor { get; set; }
        public string TextColor { get; set; }
        public string HoverTextColor { get; set; }
        public string CancelColor { get; set; }
        public string CancelHoverColor { get; set; }
        public string CancelBorderColor { get; set; }
        public string CancelHoverBorderColor { get; set; }
        public string CancelTextColor { get; set; }
        public string ShadowColor { get; set; }

        public Color ReadColor(string value, Color fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return fallback;
                string hex = value.Trim().TrimStart('#');
                if (hex.Length == 6)
                {
                    int rgb = Convert.ToInt32(hex, 16);
                    return Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
                }
                if (hex.Length == 8)
                {
                    int argb = unchecked((int)Convert.ToUInt32(hex, 16));
                    return Color.FromArgb(argb);
                }
            }
            catch { }
            return fallback;
        }

        public static WheelColorScheme CreateDefault()
        {
            return new WheelColorScheme
            {
                Name = "Revit Dark",
                BackdropColor = "#ECECEC",
                SectorColor = "#3D4248",
                SectorHoverColor = "#3F80C9",
                SectorBorderColor = "#5B626A",
                SectorHoverBorderColor = "#70A6DD",
                TabSectorColor = "#34393F",
                TabSectorHoverColor = "#356FA8",
                TabSectorBorderColor = "#50575F",
                TabSectorHoverBorderColor = "#6B98C2",
                TextColor = "#EBEEF1",
                HoverTextColor = "#FFFFFF",
                CancelColor = "#2E3237",
                CancelHoverColor = "#4E545B",
                CancelBorderColor = "#5B626A",
                CancelHoverBorderColor = "#7F8892",
                CancelTextColor = "#E2E5E8",
                ShadowColor = "#50000000"
            };
        }
    }
}
