using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.UI;
using Autodesk.Windows;

namespace RadialButton
{
    public sealed class ExternalCommandInfo
    {
        public string PluginName { get; private set; }

        public string ButtonName { get; private set; }

        public string CommandId { get; private set; }

        public string DisplayName
        {
            get { return PluginName + ": " + ButtonName; }
        }

        public ExternalCommandInfo(
            string pluginName,
            string buttonName,
            string commandId)
        {
            PluginName = pluginName;
            ButtonName = buttonName;
            CommandId = commandId;
        }
    }

    public static class ExternalCommandCatalog
    {
        private const string RadialButtonName =
            "RadialButtonSettings";

        public static IReadOnlyList<ExternalCommandInfo>
            GetCommands()
        {
            List<ExternalCommandInfo> result =
                new List<ExternalCommandInfo>();

            RibbonControl ribbon =
                ComponentManager.Ribbon;

            if (ribbon == null)
            {
                return result;
            }

            HashSet<string> commandIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (RibbonTab tab in ribbon.Tabs)
            {
                if (tab == null)
                {
                    continue;
                }

                foreach (Autodesk.Windows.RibbonPanel panel
                         in tab.Panels)
                {
                    if (panel == null ||
                        panel.Source == null)
                    {
                        continue;
                    }

                    foreach (Autodesk.Windows.RibbonButton button
                             in EnumerateButtons(panel.Source.Items))
                    {
                        // IDs are opaque: normalize labels, never identifiers.
                        string commandId =
                            button.Id;

                        if (!IsExternalCommandId(commandId) ||
                            commandId.IndexOf(
                                RadialButtonName,
                                StringComparison.OrdinalIgnoreCase) >= 0 ||
                            commandIds.Contains(commandId))
                        {
                            continue;
                        }

                        RevitCommandId revitCommandId;

                        try
                        {
                            revitCommandId =
                                RevitCommandId.LookupCommandId(
                                    commandId);
                        }
                        catch (Autodesk.Revit.Exceptions.ArgumentException)
                        {
                            continue;
                        }

                        if (revitCommandId == null)
                        {
                            continue;
                        }

                        string buttonName =
                            Clean(button.Text);

                        if (string.IsNullOrWhiteSpace(buttonName))
                        {
                            buttonName = Clean(button.Name);
                        }

                        if (string.IsNullOrWhiteSpace(buttonName))
                        {
                            continue;
                        }

                        string pluginName =
                            GetPluginName(tab, panel);

                        commandIds.Add(commandId);
                        result.Add(
                            new ExternalCommandInfo(
                                pluginName,
                                buttonName,
                                commandId));
                    }
                }
            }

            return result
                .OrderBy(
                    value => value.PluginName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    value => value.ButtonName,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<Autodesk.Windows.RibbonButton> EnumerateButtons(
            IEnumerable<Autodesk.Windows.RibbonItem> items)
        {
            foreach (Autodesk.Windows.RibbonItem item in items)
            {
                // Pulldowns and split buttons inherit RibbonListButton. Their
                // parent is a menu, not a command; only emit their child buttons.
                if (item is RibbonListButton menu)
                {
                    foreach (var child in EnumerateButtons(menu.Items))
                        yield return child;
                }
                else if (item is RibbonRowPanel row)
                {
                    foreach (var child in EnumerateButtons(row.Items))
                        yield return child;
                }
                else if (item is Autodesk.Windows.RibbonButton button)
                {
                    yield return button;
                }
            }
        }

        private static bool IsExternalCommandId(
            string commandId)
        {
            return !string.IsNullOrWhiteSpace(commandId) &&
                   commandId.IndexOf(
                       "CustomCtrl_",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetPluginName(
            RibbonTab tab,
            Autodesk.Windows.RibbonPanel panel)
        {
            string tabTitle = Clean(tab.Title);
            string panelTitle = Clean(panel.Source.Title);

            // Commands placed on Revit's shared Add-Ins tab are best
            // identified by their panel; commands on a custom tab are best
            // identified by the tab itself.
            if (IsAddInsTab(tab) &&
                !string.IsNullOrWhiteSpace(panelTitle))
            {
                return panelTitle;
            }

            if (!string.IsNullOrWhiteSpace(tabTitle))
            {
                return tabTitle;
            }

            if (!string.IsNullOrWhiteSpace(panelTitle))
            {
                return panelTitle;
            }

            return "Plugin";
        }

        private static bool IsAddInsTab(
            RibbonTab tab)
        {
            string id = Clean(tab.Id)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty);

            string title = Clean(tab.Title);

            return id.IndexOf(
                       "AddIns",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   string.Equals(
                       title,
                       "Add-Ins",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string Clean(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return Regex.Replace(
                    value,
                    "\\s+",
                    " ")
                .Trim();
        }
    }
}
