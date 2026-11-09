using System;
using System.Drawing;
using System.Globalization;

namespace RadialButton
{
    internal sealed class SettingsPalette
    {
        public Color Accent { get; private set; }
        public Color AccentHover { get; private set; }
        public Color AccentPressed { get; private set; }
        public Color AccentText { get; private set; }
        public Color Tint { get; private set; }
        public Color TintHover { get; private set; }
        public Color TintPressed { get; private set; }
        public Color Hover => Tint;
        public Color Selection => TintHover;

        public SettingsPalette(WheelColorScheme scheme)
        {
            Color source;
            if (!TryColor(scheme?.SectorHoverBorderColor, out source) &&
                !TryColor(scheme?.TabSectorHoverBorderColor, out source) &&
                !TryColor(scheme?.SectorHoverColor, out source))
                source = Color.FromArgb(73, 122, 166);
            source = Mix(Color.White, Color.FromArgb(source.R, source.G, source.B), source.A / 255.0);
            for (int step = 0; step <= 1000; step++)
            {
                Accent = Mix(source, Color.Black, step / 1000.0);
                if (Contrast(Accent, Color.White) >= 4.5) break;
            }
            AccentHover = Mix(Accent, Color.Black, 0.10);
            AccentPressed = Mix(Accent, Color.Black, 0.18);
            AccentText = Mix(Accent, Color.Black, 0.22);
            Tint = Mix(Color.White, Accent, 0.07);
            TintHover = Mix(Color.White, Accent, 0.12);
            TintPressed = Mix(Color.White, Accent, 0.18);
        }

        private static bool TryColor(string value, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(value)) return false;
            string hex = value.Trim();
            if (!hex.StartsWith("#") || (hex.Length != 7 && hex.Length != 9) ||
                !uint.TryParse(hex.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint bits))
                return false;
            color = Color.FromArgb(hex.Length == 9 ? (int)(bits >> 24) : 255,
                (int)(bits >> 16) & 255, (int)(bits >> 8) & 255, (int)bits & 255);
            return true;
        }

        internal static Color Mix(Color from, Color to, double amount) => Color.FromArgb(
            Channel(from.R, to.R, amount), Channel(from.G, to.G, amount), Channel(from.B, to.B, amount));

        private static int Channel(byte from, byte to, double amount) =>
            (int)Math.Round((1 - amount) * from + amount * to, MidpointRounding.ToEven);

        internal static double Contrast(Color first, Color second)
        {
            double a = Luminance(first), b = Luminance(second);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static double Luminance(Color color) =>
            0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

        private static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
