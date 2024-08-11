namespace TinyHalflifeServer;

internal class Logger
{
    public static void Debug(string message, params object?[] arg)
    {
        if (Program.Config!.Debug)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("debug: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(message, arg);
            Console.Write("\n");
        }
    }
    public static void Log(string message, params object?[] arg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("info: ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(message, arg);
        Console.Write("\n");
    }
    public static void Warn(string message, params object?[] arg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("warn: ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(message, arg);
        Console.Write("\n");
    }
    public static void Error(string message, params object?[] arg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("error: ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(message, arg);
        Console.Write("\n");
    }
    public static void Crit(string message, params object?[] arg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("crit: ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(message, arg);
        Console.Write("\n");
    }
}
