using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace WukongApi
{
    public static class Logging
    {
        private const string LocationPropertyName = "__Location";

        private enum LogLevel
        {
            Trace,
            Debug,
            Information,
            Warning,
            Error,
            Critical
        }

        private static readonly Regex PlaceholderRegex = new(@"\{([_\w]+)\}", RegexOptions.Compiled);

        static Logging()
        {
            Photon.Realtime.Log.Init(MakePhotonLogHandler(LogLevel.Error), MakePhotonLogHandler(LogLevel.Warning), MakePhotonLogHandler(LogLevel.Debug), MakePhotonLogHandler(LogLevel.Debug), (exception, _) => { LogException(exception); });
        }

        private static Action<string> MakePhotonLogHandler(LogLevel level) => e => { Log(level, "[Photon] {Log}", e); };

        private static void Log(LogLevel level, [StructuredMessageTemplate] string messageTemplate, params Span<object?> values)
        {
            var propertyNames = ExtractPropertyNames(messageTemplate);
            var properties = new Dictionary<string, object?>();

            for (var i = 0; i < propertyNames.Count && i < values.Length; i++)
            {
                properties[propertyNames[i]] = values[i];
            }

#if !DEBUG
            if (level is LogLevel.Error or LogLevel.Critical)
            {
#endif
                var interpolatedMessage = messageTemplate;
                foreach (var (prop, val) in properties)
                {
#if !DEBUG
                    if (prop == LocationPropertyName)
                    {
                        interpolatedMessage = interpolatedMessage.Replace($"{{{prop}}}", "<REDACTED>");
                        continue;
                    }
#endif
                    interpolatedMessage = interpolatedMessage.Replace($"{{{prop}}}", val?.ToString() ?? "null");
                }

#if !DEBUG
                Console.ForegroundColor = ConsoleColor.Red;
#else
            Console.ForegroundColor = level switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.White,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Red,
                _ => throw new ArgumentOutOfRangeException()
            };
#endif
                Console.WriteLine($"[{level}] {interpolatedMessage}");
                Console.ForegroundColor = ConsoleColor.White;
#if !DEBUG
            }
#endif

            if (level != LogLevel.Trace)
            {
                Logger.Instance.Log(messageTemplate, properties, level.ToString());
            }
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

        public static void LogTrace([StructuredMessageTemplate] string template, params Span<object?> args)
        {
            Log(LogLevel.Trace, template, args);
        }

        public static void LogDebug([StructuredMessageTemplate] string template, params Span<object?> args)
        {
            Log(LogLevel.Debug, template, args);
        }

        public static void LogInformation([StructuredMessageTemplate] string template, params Span<object?> args)
        {
            Log(LogLevel.Information, template, args);
        }

        public static void LogWarning([StructuredMessageTemplate] string template, params Span<object?> args)
        {
            Log(LogLevel.Warning, template, args);
        }

        public static void LogError([StructuredMessageTemplate] string template, params List<object?> args)
        {
            var caller = new StackFrame(1).GetMethod();
            args.Add($"{caller.DeclaringType?.FullName}.{caller.Name}");
            Log(LogLevel.Error, template + $" [at {{{LocationPropertyName}}}]", args.ToArray().AsSpan());
        }

        public static void LogCritical([StructuredMessageTemplate] string template, params List<object?> args)
        {
            var caller = new StackFrame(1).GetMethod();
            args.Add($"{caller.DeclaringType?.FullName}.{caller.Name}");
            Log(LogLevel.Critical, template + $" [at {{{LocationPropertyName}}}]", args.ToArray().AsSpan());
        }

        public static void LogException(Exception? ex)
        {
            while (ex != null)
            {
                Log(LogLevel.Error, "Exception: {Message}.\nStack trace:\n{Trace}", ex.Message, ex.StackTrace);
                ex = ex.InnerException;
            }
        }

        public static void LogCriticalException(Exception? ex)
        {
            while (ex != null)
            {
                Log(LogLevel.Critical, "Exception: {Message}.\nStack trace:\n{Trace}", ex.Message, ex.StackTrace);
                ex = ex.InnerException;
            }
        }
    }
}