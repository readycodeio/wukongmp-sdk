using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace WukongMp.Api;

/// <summary>
/// Provides static logging methods that can be used throughout the codebase without needing to inject a logger instance.
/// </summary>
[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public static class Logging
{
    /// <summary>
    /// Logs a trace message.
    /// This method is only active in DEBUG builds and will be stripped out in release builds to avoid performance overhead.
    /// </summary>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in").</param>
    /// <param name="args">The arguments to be formatted into the message template.</param>
    public static void LogTrace([StructuredMessageTemplate] string? message, params object?[] args)
#if DEBUG
        => DI.Instance.Logger.LogTrace(message, args);
#else
        {}
#endif

    /// <summary>
    /// Logs a debug message.
    /// This method is only active in DEBUG builds and will be stripped out in release builds to avoid performance overhead.
    /// </summary>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in").</param>
    /// <param name="args">The arguments to be formatted into the message template.</param>
    public static void LogDebug([StructuredMessageTemplate] string? message, params object?[] args)
#if DEBUG
        => DI.Instance.Logger.LogDebug(message, args);
#else
        {}
#endif
    
    /// <summary>
    /// Logs an informational message.
    /// This method is active in all build configurations and is suitable for logging general information about the mod's operation.
    /// </summary>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in").</param>
    /// <param name="args">The arguments to be formatted into the message template.</param>
    public static void LogInformation([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogInformation(message, args);
    
    /// <summary>
    /// Logs a warning message.
    /// This method is active in all build configurations and is suitable for logging potential issues or important events that are not necessarily errors.
    /// </summary>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in").</param>
    /// <param name="args">The arguments to be formatted into the message template.</param>
    public static void LogWarning([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogWarning(message, args);
    
    /// <summary>
    /// Logs an error message.
    /// This method is active in all build configurations and is suitable for logging errors or exceptions that occur during the mod's operation.
    /// </summary>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in").</param>
    /// <param name="args">The arguments to be formatted into the message template.</param>
    public static void LogError([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogError(message, args);

    /// <summary>
    /// Logs an error message along with an exception.
    /// This method is active in all build configurations and is suitable for logging errors or exceptions that occur during the mod's operation, providing additional context from the exception.
    /// </summary>
    /// <param name="ex">The exception to log. This can be null if no exception is being logged, but the method will still log the message and arguments.</param>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in"). This parameter is optional and can be null, in which case only the exception will be logged.</param>
    /// <param name="args">The arguments to be formatted into the message template. This parameter is optional and can be empty if no additional context is needed.</param>
    public static void LogError(Exception? ex, [StructuredMessageTemplate] string? message = null, params object?[] args)
        => DI.Instance.Logger.LogError(ex, message, args);
    
    /// <summary>
    /// Logs a critical error message.
    /// This method is active in all build configurations and is suitable for logging critical errors or exceptions that occur during the mod's operation, indicating a severe failure that may result in shutdown or data loss.
    /// </summary>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in").</param>
    /// <param name="args">The arguments to be formatted into the message template.</param>
    public static void LogCritical([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogCritical(message, args);

    /// <summary>
    /// Logs a critical error message along with an exception.
    /// This method is active in all build configurations and is suitable for logging critical errors or exceptions that occur during the mod's operation, providing additional context from the exception and indicating a severe failure that may result in shutdown or data loss.
    /// </summary>
    /// <param name="ex">The exception to log. This can be null if no exception is being logged, but the method will still log the message and arguments.</param>
    /// <param name="message">The message template to log. Use structured logging syntax (e.g., "User {UserId} logged in"). This parameter is optional and can be null, in which case only the exception will be logged, but the log entry will still be marked as critical.</param>
    /// <param name="args">The arguments to be formatted into the message template. This parameter is optional and can be empty if no additional context is needed.</param>
    public static void LogCritical(Exception? ex, [StructuredMessageTemplate] string? message = null, params object?[] args)
        => DI.Instance.Logger.LogCritical(ex, message, args);

    /// <summary>
    /// Logs an exception with an optional message and arguments.
    /// This method is a convenience wrapper around <c>LogError</c> that allows you to log an exception along with a custom message and structured arguments.
    /// If the message is null, it will default to "An exception occurred".
    /// This method is active in all build configurations and is suitable for logging exceptions that occur during the mod's operation, providing additional context from the exception and any relevant information through the message and arguments.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="message">An optional message template to log alongside the exception. Use structured logging syntax (e.g., "User {UserId} logged in"). If <c>null</c>, a default message will be used.</param>
    /// <param name="args">The arguments to be formatted into the message template. This parameter is optional and can be empty if no additional context is needed.</param>
    public static void LogException(Exception ex, [StructuredMessageTemplate] string? message = null, params object?[] args)
        => DI.Instance.Logger.LogError(ex, message ?? "An exception occurred", args);
}
