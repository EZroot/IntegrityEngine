public static class Logger
{
    private static readonly Dictionary<string, ConsoleColor> ColorMap = new Dictionary<string, ConsoleColor>
    {
        { "red", ConsoleColor.Red },
        { "green", ConsoleColor.Green },
        { "blue", ConsoleColor.Blue },
        { "yellow", ConsoleColor.Yellow },
        { "cyan", ConsoleColor.Cyan },
        { "magenta", ConsoleColor.Magenta },
        { "white", ConsoleColor.White },
        { "gray", ConsoleColor.Gray },
    };

    public enum LogSeverity
    {
        Info,
        Warning,
        Error
    }

    public const bool IS_IN_DEBUG_MODE = true;

    public static void Log(string message, LogSeverity logSeverity = LogSeverity.Info)
    {
        string debugText;
        if (IS_IN_DEBUG_MODE)
            debugText = DateTime.Now.ToString("HH:mm:ss.fff") + " - ";

        switch (logSeverity)
        {
            case LogSeverity.Info:
                WriteColored($"{debugText}<color=white>[INFO]</color>: {message}");
                break;
            case LogSeverity.Warning:
                WriteColored($"{debugText}<color=yellow>[WARNING]</color>: {message}");
                break;
            case LogSeverity.Error:
                WriteColored($"{debugText}<color=red>[ERROR]</color>: {message}");
                break;
        }
    }

    /// <summary>
    /// Parses a string with <color=NAME> tags and writes the output to the console.
    /// Non-colored text is written in the default color.
    /// </summary>
    public static void WriteColored(string input)
    {
        string remaining = input;

        while (remaining.Length > 0)
        {
            int startIndex = remaining.IndexOf("<color=");

            if (startIndex == -1)
            {
                Console.Write(remaining);
                break;
            }

            if (startIndex > 0)
            {
                Console.Write(remaining[..startIndex]);
            }

            int colorTagEnd = remaining.IndexOf('>', startIndex);
            if (colorTagEnd == -1) break;

            string tagContent = remaining.Substring(startIndex + 7, colorTagEnd - (startIndex + 7));
            string colorName = tagContent.Trim().ToLower();

            int closeTagStart = remaining.IndexOf("</color>", colorTagEnd);
            if (closeTagStart == -1) break;

            string coloredText = remaining.Substring(colorTagEnd + 1, closeTagStart - (colorTagEnd + 1));

            if (ColorMap.TryGetValue(colorName, out ConsoleColor color))
            {
                Console.ForegroundColor = color;
                Console.Write(coloredText);
                Console.ResetColor();
            }
            else
            {
                Console.Write(coloredText);
            }

            remaining = remaining.Substring(closeTagStart + "</color>".Length);
        }
        Console.WriteLine();
    }
}