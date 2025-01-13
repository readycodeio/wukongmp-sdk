using System;

namespace WukongCSharpMod
{
    public static class Helpers
    {
#if UNITY_EDITOR
        private static void Log(string message) {
            UnityEngine.Debug.Log(message);
        }

        public static void LogError(string message)
        {
            UnityEngine.Debug.Log(message);
        }
#else
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
#endif
    }
}