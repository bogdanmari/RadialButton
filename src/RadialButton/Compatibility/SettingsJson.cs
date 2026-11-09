#if NETFRAMEWORK
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace RadialButton
{
    // Keep the existing JSON format and runtime-native serializers in both builds.
    internal static class SettingsJson
    {
        public static T Deserialize<T>(string json)
        {
#if NETFRAMEWORK
            return CreateSerializer().Deserialize<T>(json);
#else
            return JsonSerializer.Deserialize<T>(json);
#endif
        }

        public static string Serialize<T>(T value)
        {
#if NETFRAMEWORK
            return CreateSerializer().Serialize(value);
#else
            return JsonSerializer.Serialize(value);
#endif
        }

#if NETFRAMEWORK
        private static JavaScriptSerializer CreateSerializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 100 };
        }
#endif
    }
}
