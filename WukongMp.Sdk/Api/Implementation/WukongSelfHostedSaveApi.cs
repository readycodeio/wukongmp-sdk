using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.Https;

namespace WukongMp.Sdk.Api.Implementation;

internal class WukongSelfHostedSaveApi : IWukongSaveApi
{
    private readonly ILogger logger;
    
    private readonly string baseUrl;
    private readonly Dictionary<string, string> headers;

    public WukongSelfHostedSaveApi(ILogger logger)
    {
        this.logger = logger;
        
        var jwtToken = WukongApi.Configuration.GetLaunchParameter("JWT_TOKEN", "");
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            logger.LogError("Invalid or missing JWT_TOKEN launch parameter");
        }

        headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {jwtToken}" }
        };

        var port = WukongApi.Configuration.GetLaunchParameter("SERVER_PORT", "9050");
        var ip = WukongApi.Configuration.GetLaunchParameter("SERVER_IP", "");

        baseUrl = $"http://{ip}:{port}"; // TODO: HTTPS support
    }

    public async Task<FileInfo?> DownloadWorldSaveAsync(CancellationToken ct = default)
    {
        var httpClient = new BouncyCastleHttpsClient(logger);
        var bytes = await httpClient.GetBytesAsync(new Uri($"{baseUrl}/api/save/{SaveFileType.WorldSave}"), headers, ct);

        if (bytes is null)
            return null;

        return new FileInfo("world.sav", bytes);
    }

    public async Task<bool> UploadWorldSaveAsync(byte[] content, CancellationToken ct = default)
    {
        var httpClient = new BouncyCastleHttpsClient(logger);
        var code = await httpClient.PutBytesAsync(new Uri($"{baseUrl}/api/save/{SaveFileType.WorldSave}"), headers, content, ct);
        return code == HttpStatusCode.NoContent;
    }

    public async Task<FileInfo?> DownloadPlayerSaveAsync(CancellationToken ct = default)
    {
        var httpClient = new BouncyCastleHttpsClient(logger);
        var bytes = await httpClient.GetBytesAsync(new Uri($"{baseUrl}/api/save/{SaveFileType.PlayerSave}"), headers, ct);

        if (bytes is null)
            return null;

        return new FileInfo("player.sav", bytes);
    }

    public async Task<bool> UploadPlayerSaveAsync(byte[] content, CancellationToken ct = default)
    {
        var httpClient = new BouncyCastleHttpsClient(logger);
        var code = await httpClient.PutBytesAsync(new Uri($"{baseUrl}/api/save/{SaveFileType.PlayerSave}"), headers, content, ct);
        return code == HttpStatusCode.NoContent;
    }
}