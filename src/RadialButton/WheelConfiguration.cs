using System.Collections.Generic;

namespace RadialButton
{
    public sealed class WheelConfiguration
    {
        public string Product { get; set; }

        public int Version { get; set; }

        public List<WheelItemSettings> Items { get; set; }

        public List<WheelTabSettings> Tabs { get; set; }

        public WheelConfiguration()
        {
            Version = 1;
            Items = new List<WheelItemSettings>();
            Tabs = new List<WheelTabSettings>();
        }
    }

    public sealed class WheelTabSettings
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsMain { get; set; }
        public bool IsFamily { get; set; }
        public List<WheelItemSettings> Items { get; set; }

        public WheelTabSettings()
        {
            Items = new List<WheelItemSettings>();
        }
    }

    public sealed class WheelItemSettings
    {
        public int Position { get; set; }

        public string Title { get; set; }

        public string CommandType { get; set; }

        public string Command { get; set; }

        public string CommandLabel { get; set; }
    }
}
