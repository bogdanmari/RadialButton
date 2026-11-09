using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace RadialButton
{
    public sealed class App : IExternalApplication
    {
        private const string ProductName = "RadialButton";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                RibbonPanel panel = application.CreateRibbonPanel(ProductName);

                PushButtonData settingsButtonData = new PushButtonData(
                    "RadialButtonSettings",
                    "RadialButton\nSettings",
                    assemblyPath,
                    typeof(RadialButtonSettingsCommand).FullName);

                settingsButtonData.ToolTip = "Don't reach across the entire screen for the Ribbon — your main Revit commands are always at the cursor.";
                settingsButtonData.LongDescription =
                    "Configure radial-menu tabs, commands, profiles and appearance.";

                ImageSource smallImage = TryLoadEmbeddedPng("RadialButton_16.png");
                ImageSource largeImage = TryLoadEmbeddedPng("RadialButton_32.png");
                if (smallImage != null) settingsButtonData.Image = smallImage;
                if (largeImage != null) settingsButtonData.LargeImage = largeImage;

                panel.AddItem(settingsButtonData);
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                TaskDialog.Show(ProductName,
                    "Failed to create the RadialButton Settings panel.\n\n" + exception.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static ImageSource TryLoadEmbeddedPng(string fileName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(
                    name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(resourceName)) return null;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
