using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace WukongApi
{
    public static class Logging
    {
        private enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        private static readonly Regex PlaceholderRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

        private static void Log(LogLevel level, [StructuredMessageTemplate] string messageTemplate, params Span<object> values)
        {
            var propertyNames = ExtractPropertyNames(messageTemplate);
            var properties = new Dictionary<string, object>();

            for (var i = 0; i < propertyNames.Count && i < values.Length; i++)
            {
                properties[propertyNames[i]] = values[i];
            }

#if !DEBUG
            if (level == LogLevel.Error)
            {
#endif
                var interpolatedMessage = messageTemplate;
                foreach (var (prop, val) in properties)
                {
                    interpolatedMessage = interpolatedMessage.Replace("{" + prop + "}", val?.ToString() ?? "null");
                }

#if !DEBUG
                Console.ForegroundColor = ConsoleColor.Red;
#else
                Console.ForegroundColor = level switch
                {
                    LogLevel.Debug => ConsoleColor.Gray,
                    LogLevel.Info => ConsoleColor.White,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => throw new ArgumentOutOfRangeException()
                };
#endif
                Console.WriteLine($"[{level}] {interpolatedMessage}");
                Console.ForegroundColor = ConsoleColor.White;
#if !DEBUG
            }
#endif

            Logger.Instance.Log(messageTemplate, properties, level.ToString());
        }

        private static List<string> ExtractPropertyNames(string template)
        {
            var matches = PlaceholderRegex.Matches(template);
            var names = new List<string>();
            foreach (Match match in matches)
            {
                names.Add(match.Groups[1].Value);
            }

            return names;
        }

        public static void LogDebug([StructuredMessageTemplate] string template, params object[] args)
        {
            Log(LogLevel.Debug, template, args);
        }

        public static void LogWarning([StructuredMessageTemplate] string template, params object[] args)
        {
            Log(LogLevel.Warning, template, args);
        }

        public static void LogError([StructuredMessageTemplate] string template, params object[] args)
        {
            Log(LogLevel.Error, template, args);
        }

        public static void LogException(Exception ex)
        {
            Log(LogLevel.Error, "Exception: {Message}.\nStack trace:\n{Trace}", ex.Message, ex.StackTrace);
        }
    }
}