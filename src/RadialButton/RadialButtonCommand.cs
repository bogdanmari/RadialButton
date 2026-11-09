using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using WinFormsControl =
    System.Windows.Forms.Control;

using WinFormsCursor =
    System.Windows.Forms.Cursor;

using WinFormsDialogResult =
    System.Windows.Forms.DialogResult;

using WinFormsKeys =
    System.Windows.Forms.Keys;

using DrawingPoint =
    System.Drawing.Point;

namespace RadialButton
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class RadialButtonCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApplication =
                    commandData.Application;

                bool shiftPressed =
                    (WinFormsControl.ModifierKeys &
                     WinFormsKeys.Shift) ==
                    WinFormsKeys.Shift;

                if (shiftPressed)
                {
                    using (WheelSettingsForm settingsForm =
                           new WheelSettingsForm())
                    {
                        settingsForm.ShowDialog(new RevitWindowOwner(uiApplication.MainWindowHandle));
                    }

                    return Result.Succeeded;
                }

                DrawingPoint cursorPosition =
                    WinFormsCursor.Position;

                if (WheelConfigurationService.GetConfigurationNames().Count == 0)
                {
                    TaskDialog.Show(
                        "RadialButton",
                        "No configuration exists. Hold Shift while launching RadialButton " +
                        "to create or import one.");

                    return Result.Cancelled;
                }

                WheelConfiguration configuration =
                    WheelConfigurationService.Load();

                Document document = uiApplication.ActiveUIDocument != null
                    ? uiApplication.ActiveUIDocument.Document
                    : null;

                bool isFamilyDocument =
                    document != null && document.IsFamilyDocument;

                WheelTabSettings currentTab =
                    configuration.Tabs.Find(value =>
                        isFamilyDocument ? value.IsFamily : value.IsMain);

                WheelItem selectedItem = null;
                Stack<string> tabHistory = new Stack<string>();
                UserInterfaceSettings interfaceSettings = UserInterfaceSettingsService.Load();

                while (selectedItem == null)
                {
                    IReadOnlyList<WheelItem> items =
                        WheelConfigurationService.CreateWheelItems(
                            configuration,
                            currentTab.Id);

                    using (WheelForm wheelForm = new WheelForm(
                        items, cursorPosition, interfaceSettings, tabHistory.Count > 0))
                    {
                        WinFormsDialogResult dialogResult = wheelForm.ShowDialog();
                        if (wheelForm.BackRequested)
                        {
                            string previousTabId = tabHistory.Pop();
                            currentTab = configuration.Tabs.Find(value =>
                                string.Equals(value.Id, previousTabId, StringComparison.OrdinalIgnoreCase));
                            if (currentTab == null)
                            {
                                TaskDialog.Show("RadialButton", "A previous tab no longer exists.");
                                return Result.Failed;
                            }
                            continue;
                        }
                        if (dialogResult != WinFormsDialogResult.OK || wheelForm.SelectedItem == null)
                            return Result.Cancelled;

                        WheelItem choice = wheelForm.SelectedItem;
                        if (choice.IsTab)
                        {
                            tabHistory.Push(currentTab.Id);
                            currentTab = configuration.Tabs.Find(
                                value => string.Equals(value.Id, choice.Command, StringComparison.OrdinalIgnoreCase));
                            if (currentTab == null)
                            {
                                TaskDialog.Show("RadialButton", "The selected tab no longer exists.");
                                return Result.Failed;
                            }
                            continue;
                        }

                        selectedItem = choice;
                    }
                }

                RevitCommandId commandId;

                if (selectedItem.CommandType ==
                    WheelCommandType.Revit)
                {
                    PostableCommand postableCommand;

                    if (!Enum.TryParse(
                            selectedItem.Command,
                            out postableCommand))
                    {
                        TaskDialog.Show(
                            "RadialButton",
                            "The selected Revit command is invalid.");

                        return Result.Failed;
                    }

                    commandId =
                        RevitCommandId
                            .LookupPostableCommandId(
                                postableCommand);
                }
                else
                {
                    commandId =
                        RevitCommandId.LookupCommandId(
                            selectedItem.Command);
                }

                if (commandId == null)
                {
                    TaskDialog.Show(
                        "RadialButton",
                        "The selected Revit command was not found.");

                    return Result.Failed;
                }

                if (!uiApplication.CanPostCommand(commandId))
                {
                    TaskDialog.Show(
                        "RadialButton",
                        "The command \"" +
                        selectedItem.Title +
                        "\" is not available in the current Revit context.");

                    return Result.Cancelled;
                }

                uiApplication.PostCommand(commandId);

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                message = exception.ToString();

                TaskDialog.Show(
                    "RadialButton",
                    "An error occurred.\n\n" +
                    exception.Message);

                return Result.Failed;
            }
        }
    }
}
