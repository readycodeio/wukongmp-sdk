using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace WukongMp.Api;

[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public static class Logging
{
    public static void LogTrace([StructuredMessageTemplate] string? message, params object?[] args)
#if DEBUG
        => DI.Instance.Logger.LogTrace(message, args);
#else
        {}
#endif

    public static void LogDebug([StructuredMessageTemplate] string? message, params object?[] args)
#if DEBUG
        => DI.Instance.Logger.LogDebug(message, args);
#else
        {}
#endif
    
    public static void LogInformation([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogInformation(message, args);
    
    public static void LogWarning([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogWarning(message, args);
    
    public static void LogError([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogError(message, args);

    public static void LogError(Exception? ex, [StructuredMessageTemplate] string? message = null, params object?[] args)
        => DI.Instance.Logger.LogError(ex, message, args);
    
    public static void LogCritical([StructuredMessageTemplate] string? message, params object?[] args)
        => DI.Instance.Logger.LogCritical(message, args);

    public static void LogCritical(Exception? ex, [StructuredMessageTemplate] string? message = null, params object?[] args)
        => DI.Instance.Logger.LogCritical(ex, message, args);

    public static void LogException(Exception ex, [StructuredMessageTemplate] string? message = null, params object?[] args)
        => DI.Instance.Logger.LogError(ex, message ?? "An exception occurred", args);

    public static void LogNull(string propertyName)
        => DI.Instance.Logger.LogNull(propertyName);
}
