using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using JetBrains.Annotations;
using ReadyM.Relay.Client;

namespace WukongMp.Api.Old
{
    public static class Logging
    {
        private const string ThreadIdPropertyName = "__ThreadId";
        private const string LocationPropertyName = "__Location";

        private static readonly Regex PlaceholderRegex = new(@"\{([_\w]+)\}", RegexOptions.Compiled);

        public static void Log(LogLevel level, [StructuredMessageTemplate] string messageTemplate, params Span<object?> values)
        {
            var propertyNames = ExtractPropertyNames(messageTemplate);
            var properties = new Dictionary<string, object?>();

            for (var i = 0; i < propertyNames.Count && i < values.Length; i++)
            {
                // if we are dealing with enums, we want to log the string representation of the enum
                if (values[i] is Enum enumValue)
                {
                    properties[propertyNames[i]] = enumValue.ToString();
                }
                else
                {
                    properties[propertyNames[i]] = values[i];
                }
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
#if TRACE_LOGS
            Log(LogLevel.Trace, template, args);
#endif
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
            var threadId = Thread.CurrentThread.ManagedThreadId;
            args.Add(threadId);
            args.Add($"{caller.DeclaringType?.FullName}.{caller.Name}");
            Log(LogLevel.Error, $"{template} [thread {{{ThreadIdPropertyName}}} at {{{LocationPropertyName}}}]", args.ToArray().AsSpan());
        }

        public static void LogCritical([StructuredMessageTemplate] string template, params List<object?> args)
        {
            var caller = new StackFrame(1).GetMethod();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            args.Add(threadId);
            args.Add($"{caller.DeclaringType?.FullName}.{caller.Name}");
            Log(LogLevel.Critical, $"{template} [thread {{{ThreadIdPropertyName}}} at {{{LocationPropertyName}}}]", args.ToArray().AsSpan());
        }

        public static void LogException(Exception? ex)
        {
            while (ex != null)
            {
                Log(LogLevel.Error, "Exception: {Message} | Thread: {ThreadId} | Stack trace: {Trace}", ex.Message, Thread.CurrentThread.ManagedThreadId, ex.StackTrace);
                ex = ex.InnerException;
            }
        }

        public static void LogCriticalException(Exception? ex)
        {
            while (ex != null)
            {
                Log(LogLevel.Critical, "Exception: {Message} | Thread: {ThreadId} | Stack trace: {Trace}", ex.Message, Thread.CurrentThread.ManagedThreadId, ex.StackTrace);
                ex = ex.InnerException;
            }
        }
    }
}