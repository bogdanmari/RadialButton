using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RadialButton
{
    internal static class IconResources
    {
        public static Icon LoadApplicationIcon()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(
                    name => name.EndsWith("RadialButton.ico", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(resourceName)) return null;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    using (Icon icon = new Icon(stream))
                    {
                        return (Icon)icon.Clone();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
