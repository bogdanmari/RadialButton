using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RadialButton
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class RadialButtonSettingsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            using (WheelSettingsForm settingsForm = new WheelSettingsForm())
            {
                settingsForm.ShowDialog(new RevitWindowOwner(commandData.Application.MainWindowHandle));
            }

            return Result.Succeeded;
        }
    }
}
