namespace RadialButton
{
    public enum WheelCommandType
    {
        Revit,
        External,
        Tab
    }

    /// <summary>
    /// Описание одного сектора кругового меню.
    /// </summary>
    public sealed class WheelItem
    {
        public string Title { get; }
        public WheelCommandType CommandType { get; }
        public string Command { get; }

        public bool IsTab
        {
            get { return CommandType == WheelCommandType.Tab; }
        }

        public WheelItem(string title, WheelCommandType commandType, string command)
        {
            Title = title;
            CommandType = commandType;
            Command = command;
        }
    }
}
