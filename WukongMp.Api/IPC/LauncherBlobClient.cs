using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client.Blobs;
using ReadyM.Relay.Client.Blobs;

namespace WukongMp.Api.IPC;

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

        using var client = new HttpClient();

        // Download
        var response = await client.GetByteArrayAsync($"http://localhost:5005/api/download/{serverId}/{nameEscaped}");

        if (response is null)
        {
            logger.LogError("Failed to download blob '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        return new BlobInfo(name, response);
    }
}