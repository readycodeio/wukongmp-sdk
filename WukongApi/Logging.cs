using System;

namespace WukongApi
{
    public static class Logging
    {
        public static void LogDebug(string message)
        {
#if DEBUG
            Console.WriteLine(message);
#endif
        }

        public static void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}