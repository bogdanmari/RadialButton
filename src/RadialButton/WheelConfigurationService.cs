using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Autodesk.Revit.UI;

namespace RadialButton
{
    public static class WheelConfigurationService
    {
        private const string ProductName = "RadialButton";
        private const string LegacyProductName = "OrbitKey";
        private const int CurrentVersion = 4;

        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "RadialButton");

        private static readonly string ConfigurationsDirectory =
            Path.Combine(
                SettingsDirectory,
                "Configurations");

        private static readonly string HelpDirectory =
            Path.Combine(
                SettingsDirectory,
                "Help");

        private static readonly string StateFilePath =
            Path.Combine(SettingsDirectory, "state.json");

        public static string DefaultSettingsPath
        {
            get { return GetConfigurationPath(GetActiveConfigurationName()); }
        }

        public static WheelConfiguration Load()
        {
            EnsureConfigurationStore();

            string activeName = GetActiveConfigurationName();
            if (!string.IsNullOrWhiteSpace(activeName))
            {
                string activePath = GetConfigurationPath(activeName);
                if (File.Exists(activePath))
                {
                    try { return LoadFromFile(activePath); }
                    catch { }
                }
            }

            List<string> names = GetConfigurationNames();
            if (names.Count > 0)
            {
                SetActiveConfigurationName(names[0]);
                return LoadFromFile(GetConfigurationPath(names[0]));
            }

            throw new InvalidOperationException(
                "No RadialButton configuration exists. Create a new configuration in Settings.");
        }

        public static List<string> GetConfigurationNames()
        {
            EnsureConfigurationStore();

            List<string> result = new List<string>();
            foreach (string path in Directory.GetFiles(ConfigurationsDirectory, "*.json"))
            {
                try
                {
                    LoadFromFile(path);
                    result.Add(Path.GetFileNameWithoutExtension(path));
                }
                catch
                {
                    // A JSON file without a RadialButton marker is not a configuration.
                }
            }

            return result.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string GetActiveConfigurationName()
        {
            EnsureConfigurationStore();

            try
            {
                if (File.Exists(StateFilePath))
                {
                    ConfigurationState state = SettingsJson.Deserialize<ConfigurationState>(
                        File.ReadAllText(StateFilePath, Encoding.UTF8));

                    if (state != null && !string.IsNullOrWhiteSpace(state.ActiveConfiguration))
                    {
                        return state.ActiveConfiguration;
                    }
                }
            }
            catch { }

            return null;
        }

        public static void SetActiveConfigurationName(string name)
        {
            ValidateConfigurationName(name);
            Directory.CreateDirectory(SettingsDirectory);

            string json = SettingsJson.Serialize(new ConfigurationState
            {
                ActiveConfiguration = name.Trim()
            });

            File.WriteAllText(StateFilePath, FormatJson(json), new UTF8Encoding(false));
        }

        public static void ClearActiveConfiguration()
        {
            Directory.CreateDirectory(SettingsDirectory);

            string json = SettingsJson.Serialize(new ConfigurationState
            {
                ActiveConfiguration = null
            });

            File.WriteAllText(StateFilePath, FormatJson(json), new UTF8Encoding(false));
        }

        public static void DeleteConfiguration(string name)
        {
            string path = GetConfigurationPath(name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static string GetConfigurationPath(string name)
        {
            ValidateConfigurationName(name);
            return Path.Combine(ConfigurationsDirectory, name.Trim() + ".json");
        }

        public static WheelConfiguration Load(string name)
        {
            return LoadFromFile(GetConfigurationPath(name));
        }

        public static WheelConfiguration LoadFromFile(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "The configuration path is empty.",
                    nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "The configuration file was not found.",
                    filePath);
            }

            string json =
                File.ReadAllText(
                    filePath,
                    Encoding.UTF8);

            WheelConfiguration configuration =
                SettingsJson.Deserialize<WheelConfiguration>(
                    json);

            UpgradeConfiguration(configuration);
            Validate(configuration);

            SortTabs(configuration);

            return configuration;
        }

        public static void Save(
            WheelConfiguration configuration)
        {
            SaveAs(GetActiveConfigurationName(), configuration);
        }

        public static void SaveAs(string name, WheelConfiguration configuration)
        {
            SaveToFile(configuration, GetConfigurationPath(name));
        }

        public static void SaveToFile(
            WheelConfiguration configuration,
            string filePath)
        {
            if (configuration != null)
            {
                configuration.Product = ProductName;
                UpgradeConfiguration(configuration);
            }

            Validate(configuration);

            string directory =
                Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            configuration.Version = CurrentVersion;

            SortTabs(configuration);

            string json =
                SettingsJson.Serialize(configuration);

            File.WriteAllText(
                filePath,
                FormatJson(json),
                new UTF8Encoding(false));
        }

        public static WheelConfiguration CreateDefault()
        {
            return new WheelConfiguration
            {
                Product = ProductName,
                Version = CurrentVersion,

                Items = new List<WheelItemSettings>(),
                Tabs = new List<WheelTabSettings>
                {
                    new WheelTabSettings
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "Main",
                        IsMain = true,
                        IsFamily = true,
                        Items = new List<WheelItemSettings>
                        {
                    CreateItem(
                        0,
                        "Move",
                        PostableCommand.Move),

                    CreateItem(
                        1,
                        "Copy",
                        PostableCommand.Copy)
                        }
                    }
                }
            };
        }

        public static IReadOnlyList<WheelItem>
            CreateWheelItems(
                WheelConfiguration configuration)
        {
            Validate(configuration);

            WheelTabSettings mainTab = configuration.Tabs.First(value => value.IsMain);
            return CreateWheelItems(configuration, mainTab.Id);
        }

        public static IReadOnlyList<WheelItem>
            CreateWheelItems(
                WheelConfiguration configuration,
                string tabId)
        {
            Validate(configuration);

            WheelTabSettings tab = configuration.Tabs.FirstOrDefault(
                value => string.Equals(value.Id, tabId, StringComparison.OrdinalIgnoreCase));

            if (tab == null)
            {
                throw new InvalidOperationException("The selected tab no longer exists.");
            }

            List<WheelItem> result =
                new List<WheelItem>();

            foreach (WheelItemSettings item
                     in tab.Items
                         .OrderBy(value => value.Position))
            {
                result.Add(
                    new WheelItem(
                        item.Title,
                        GetCommandType(item),
                        item.Command));
            }

            return result;
        }

        public static void Validate(
            WheelConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new InvalidOperationException(
                    "The configuration is empty.");
            }

            if (!string.Equals(configuration.Product, ProductName, StringComparison.Ordinal) &&
                !string.Equals(configuration.Product, LegacyProductName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "This file is not a RadialButton configuration.");
            }

            UpgradeConfiguration(configuration);

            if (configuration.Tabs == null || configuration.Tabs.Count == 0)
            {
                throw new InvalidOperationException(
                    "The configuration must contain at least one tab.");
            }

            if (configuration.Tabs.Count(value => value.IsMain) != 1)
            {
                throw new InvalidOperationException(
                    "Exactly one tab must be marked as Main Tab.");
            }

            if (configuration.Tabs.Count(value => value.IsFamily) != 1)
            {
                throw new InvalidOperationException(
                    "Exactly one tab must be marked as Family Tab.");
            }

            HashSet<string> tabIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> tabNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (WheelTabSettings tab in configuration.Tabs)
            {
                if (tab == null || string.IsNullOrWhiteSpace(tab.Id) ||
                    string.IsNullOrWhiteSpace(tab.Name))
                {
                    throw new InvalidOperationException(
                        "Each tab must have an id and a name.");
                }
                if (!tabIds.Add(tab.Id) || !tabNames.Add(tab.Name.Trim()))
                {
                    throw new InvalidOperationException("Tab names and ids must be unique.");
                }
                if (tab.Items == null || tab.Items.Count < 2 || tab.Items.Count > 8)
                {
                    throw new InvalidOperationException("Each tab must contain between two and eight items.");
                }
                HashSet<int> positions = new HashSet<int>();
                foreach (WheelItemSettings item in tab.Items)
                {
                    if (item == null || item.Position < 0 || item.Position >= tab.Items.Count ||
                        !positions.Add(item.Position))
                    {
                        throw new InvalidOperationException("Item positions in tab \"" + tab.Name + "\" are invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Command))
                    {
                        throw new InvalidOperationException("Every sector must have a displayed name and command.");
                    }
                    WheelCommandType commandType = GetCommandType(item);
                    if (commandType == WheelCommandType.Revit)
                    {
                        PostableCommand command;
                        if (!Enum.TryParse(item.Command, out command))
                            throw new InvalidOperationException("Invalid Revit command: " + item.Command);
                    }
                    else if (commandType == WheelCommandType.Tab &&
                             !configuration.Tabs.Any(value => string.Equals(value.Id, item.Command, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("A sector in tab \"" + tab.Name + "\" links to a missing tab.");
                    }
                }
            }
        }

        private static void UpgradeConfiguration(WheelConfiguration configuration)
        {
            if (configuration == null) return;
            if (string.Equals(configuration.Product, LegacyProductName, StringComparison.Ordinal))
                configuration.Product = ProductName;
            if (configuration.Tabs == null) configuration.Tabs = new List<WheelTabSettings>();
            if (configuration.Tabs.Count == 0 && configuration.Items != null && configuration.Items.Count > 0)
            {
                configuration.Tabs.Add(new WheelTabSettings
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "Main",
                    IsMain = true,
                    IsFamily = true,
                    Items = configuration.Items
                });
                configuration.Items = new List<WheelItemSettings>();
            }

            if (configuration.Tabs.Count > 0 &&
                !configuration.Tabs.Any(value => value.IsFamily))
            {
                WheelTabSettings familyTab =
                    configuration.Tabs.FirstOrDefault(value => value.IsMain) ??
                    configuration.Tabs[0];

                familyTab.IsFamily = true;
            }
        }

        private static void SortTabs(WheelConfiguration configuration)
        {
            configuration.Items = new List<WheelItemSettings>();
            foreach (WheelTabSettings tab in configuration.Tabs)
                tab.Items = tab.Items.OrderBy(item => item.Position).ToList();
        }

        private static WheelItemSettings CreateItem(
            int position,
            string title,
            PostableCommand command)
        {
            return new WheelItemSettings
            {
                Position = position,
                Title = title,
                CommandType =
                    WheelCommandType.Revit.ToString(),
                Command = command.ToString(),
                CommandLabel = command.ToString()
            };
        }

        private static WheelCommandType GetCommandType(
            WheelItemSettings item)
        {
            if (string.IsNullOrWhiteSpace(item.CommandType))
            {
                // Version 1 configurations contained only PostableCommand
                // values, so missing type information means a Revit command.
                return WheelCommandType.Revit;
            }

            WheelCommandType commandType;

            if (!Enum.TryParse(
                    item.CommandType,
                    true,
                    out commandType))
            {
                throw new InvalidOperationException(
                    "Invalid command type: " +
                    item.CommandType);
            }

            return commandType;
        }

        public static void ValidateConfigurationName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("The configuration name cannot be empty.");
            }

            string trimmed = name.Trim();
            if (trimmed == "." || trimmed == ".." ||
                trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("The configuration name contains invalid characters.");
            }
        }

        private static void EnsureConfigurationStore()
        {
            Directory.CreateDirectory(ConfigurationsDirectory);
            Directory.CreateDirectory(HelpDirectory);

            if (Directory.GetFiles(ConfigurationsDirectory, "*.json").Length > 0)
            {
                return;
            }

            // A fresh installation starts with Default. Once state.json exists,
            // an empty Configurations folder is intentional and remains empty
            // until the user creates or imports a configuration.
            if (!File.Exists(StateFilePath))
            {
                SaveAs("Default", CreateDefault());
                SetActiveConfigurationName("Default");
            }
        }

        private sealed class ConfigurationState
        {
            public ConfigurationState() { }
            public string ActiveConfiguration { get; set; }
        }

        internal static string FormatJsonForSettings(string json)
        {
            return FormatJson(json);
        }

        private static string FormatJson(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            StringBuilder result =
                new StringBuilder();

            bool insideString = false;
            bool escaping = false;
            int indentation = 0;

            foreach (char character in json)
            {
                if (escaping)
                {
                    result.Append(character);
                    escaping = false;
                    continue;
                }

                if (character == '\\' && insideString)
                {
                    result.Append(character);
                    escaping = true;
                    continue;
                }

                if (character == '"')
                {
                    insideString = !insideString;
                    result.Append(character);
                    continue;
                }

                if (insideString)
                {
                    result.Append(character);
                    continue;
                }

                switch (character)
                {
                    case '{':
                    case '[':
                        result.Append(character);
                        result.AppendLine();
                        indentation++;
                        AppendIndentation(
                            result,
                            indentation);
                        break;

                    case '}':
                    case ']':
                        result.AppendLine();
                        indentation--;
                        AppendIndentation(
                            result,
                            indentation);
                        result.Append(character);
                        break;

                    case ',':
                        result.Append(character);
                        result.AppendLine();
                        AppendIndentation(
                            result,
                            indentation);
                        break;

                    case ':':
                        result.Append(": ");
                        break;

                    default:
                        if (!char.IsWhiteSpace(character))
                        {
                            result.Append(character);
                        }

                        break;
                }
            }

            return result.ToString();
        }

        private static void AppendIndentation(
            StringBuilder builder,
            int indentation)
        {
            builder.Append(
                new string(
                    ' ',
                    indentation * 2));
        }
    }
}
