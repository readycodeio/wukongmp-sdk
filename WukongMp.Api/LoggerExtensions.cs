using Microsoft.Extensions.Logging;

namespace WukongMp.Api;

internal static class LoggerExtensions
{
    public static void LogNullDebug(this ILogger logger, string propertyName)
        => logger.LogDebug("{Value} is null", propertyName);

    public static void LogNull(this ILogger logger, string propertyName)
    => logger.LogError("{Value} is null", propertyName);
}
