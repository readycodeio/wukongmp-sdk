using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client.Blobs;
using ReadyM.Relay.Client.Blobs;

namespace WukongMp.Api.Https;

public class LauncherBlobClient(ILogger logger)
    : IBlobClient
{
    private bool _webAlreadyInit;

    private void InitWebRequest()
    {
        if (_webAlreadyInit)
            return;
        _webAlreadyInit = true;

        if (WebRequest.RegisterPrefix("http", GetWebCreator()))
            logger.LogDebug("Registered http:// prefix");
        if (WebRequest.RegisterPrefix("https", GetWebCreator()))
            logger.LogDebug("Registered https:// prefix");
    }

    private static IWebRequestCreate GetWebCreator()
    {
        var type = Type.GetType("System.Net.HttpRequestCreator, System, Version=4.0.0.0,Culture=neutral, PublicKeyToken=b77a5c561934e089");
        Debug.Assert(type != null);
        return (IWebRequestCreate)Activator.CreateInstance(type, nonPublic: true);
    }

    public async Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default)
    {
        InitWebRequest();
        return false;
    }

    public async Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        InitWebRequest();

        var serverId = CmdLineParams.Instance.ServerId!.Value;
        var nameEscaped = Uri.EscapeDataString(name);

        var client = new BouncyCastleHttpsClient();

        // Download
        var linkUrl = new Uri($"https://api.ready.mp/api/server/{serverId}/files/{nameEscaped}");
        var downloadResponse = await client.GetAsync<DownloadServerFileResponse>(linkUrl, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {CmdLineParams.Instance.JwtToken}" }
        }, ct);

        if (downloadResponse is null)
        {
            logger.LogWarning("Failed to get download URL for blob '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        var downloadUrl = new Uri(downloadResponse.DownloadUrl);
        var response = await BouncyCastleHttpsClient.GetBytesAsync(downloadUrl);

        if (response == null)
        {
            logger.LogError("Failed to download blob content '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        return new BlobInfo(name, response);
    }
}