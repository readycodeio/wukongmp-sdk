using Microsoft.Extensions.Logging;

namespace WukongMp.Api;

public static class LoggerExtensions
{
    public static void LogNull(this ILogger logger, string propertyName)
        => logger.LogError("{Value} is null", propertyName);
}
