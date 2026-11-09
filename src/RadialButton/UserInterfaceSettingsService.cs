using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace RadialButton
{
    public static class UserInterfaceSettingsService
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RadialButton");

        public static readonly string SettingsFilePath =
            Path.Combine(SettingsDirectory, "ui-settings.json");

        public static UserInterfaceSettings Load()
        {
            return Load(true);
        }

        internal static UserInterfaceSettings LoadForAppearance()
        {
            return Load(false);
        }

        private static UserInterfaceSettings Load(bool createDefaultFile)
        {
            UserInterfaceSettings fallback = CreateDefault();
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    if (createDefaultFile) Save(fallback);
                    return fallback;
                }

                UserInterfaceSettings settings = SettingsJson.Deserialize<UserInterfaceSettings>(
                    File.ReadAllText(SettingsFilePath, Encoding.UTF8));

                if (settings == null) return fallback;
                if (settings.ColorSchemes == null || settings.ColorSchemes.Count == 0)
                    settings.ColorSchemes = fallback.ColorSchemes;
                settings.ColorSchemes.RemoveAll(value => value == null || string.IsNullOrWhiteSpace(value.Name));
                if (settings.ColorSchemes.Count == 0) settings.ColorSchemes = fallback.ColorSchemes;

                if (string.IsNullOrWhiteSpace(settings.SelectedColorScheme) ||
                    settings.ColorSchemes.Find(value => string.Equals(value.Name, settings.SelectedColorScheme,
                        StringComparison.OrdinalIgnoreCase)) == null)
                    settings.SelectedColorScheme = settings.ColorSchemes[0].Name;

                // There is deliberately no maximum. Only values that cannot
                // produce a usable window fall back to the default.
                if (double.IsNaN(settings.WheelScale) || double.IsInfinity(settings.WheelScale) ||
                    settings.WheelScale <= 0.0)
                    settings.WheelScale = fallback.WheelScale;

                if (string.IsNullOrWhiteSpace(settings.KeyboardBackButton))
                    settings.KeyboardBackButton = fallback.KeyboardBackButton;
                if (string.IsNullOrWhiteSpace(settings.MouseBackButton))
                    settings.MouseBackButton = fallback.MouseBackButton;

                return settings;
            }
            catch
            {
                return fallback;
            }
        }

        public static void Save(UserInterfaceSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            Directory.CreateDirectory(SettingsDirectory);
            string json = SettingsJson.Serialize(settings);
            File.WriteAllText(SettingsFilePath, WheelConfigurationService.FormatJsonForSettings(json),
                new UTF8Encoding(false));
        }

        public static UserInterfaceSettings CreateDefault()
        {
            WheelColorScheme dark = WheelColorScheme.CreateDefault();
            WheelColorScheme light = new WheelColorScheme
            {
                Name = "Revit Light",
                BackdropColor = "#E1E4E7",
                SectorColor = "#F4F5F6",
                SectorHoverColor = "#D7E8F8",
                SectorBorderColor = "#A5ABB1",
                SectorHoverBorderColor = "#5E9DD5",
                TabSectorColor = "#E8EDF1",
                TabSectorHoverColor = "#C9DFF2",
                TabSectorBorderColor = "#8F9DA8",
                TabSectorHoverBorderColor = "#4F8FC7",
                TextColor = "#2B3035",
                HoverTextColor = "#173A59",
                CancelColor = "#E3E6E8",
                CancelHoverColor = "#CBD0D4",
                CancelBorderColor = "#9BA2A8",
                CancelHoverBorderColor = "#737C84",
                CancelTextColor = "#30353A",
                ShadowColor = "#50000000"
            };

            return new UserInterfaceSettings
            {
                SelectedColorScheme = dark.Name,
                WheelScale = 1.0,
                KeyboardBackButton = "Back",
                MouseBackButton = "None",
                ColorSchemes = new List<WheelColorScheme> { dark, light }
            };
        }
    }
}
