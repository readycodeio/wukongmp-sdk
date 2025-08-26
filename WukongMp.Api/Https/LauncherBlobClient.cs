using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client.Blobs;
using ReadyM.Relay.Client.Blobs;

namespace WukongMp.Api.Https;

public class LauncherBlobClient(ILogger logger) : IBlobClient
{
    public async Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default)
    {
        var serverId = CmdLineParams.Instance.ServerId!.Value;
        var nameEscaped = Uri.EscapeDataString(blob.Name);

        var client = new BouncyCastleHttpsClient();

        var url = new Uri($"{CmdLineParams.Instance.ApiBaseUrl}/api/server/{serverId}/files/{nameEscaped}");

        var status = await client.PutMultipartAsync(url, [], "file", blob.Name, blob.Content, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {CmdLineParams.Instance.JwtToken}" }
        }, ct);

        return status is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous;
    }

    public async Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        var serverId = CmdLineParams.Instance.ServerId!.Value;
        var nameEscaped = Uri.EscapeDataString(name);

        var client = new BouncyCastleHttpsClient();

        // Download
        var linkUrl = new Uri($"{CmdLineParams.Instance.ApiBaseUrl}/api/server/{serverId}/files/{nameEscaped}");
        var downloadResponse = await client.GetAsync<DownloadServerFileResponse>(linkUrl, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {CmdLineParams.Instance.JwtToken}" }
        }, ct);

        if (string.IsNullOrWhiteSpace(downloadResponse?.DownloadUrl))
        {
            logger.LogWarning("Failed to get download URL for blob '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        var downloadUrl = new Uri(downloadResponse!.DownloadUrl);
        var response = await client.GetBytesAsync(downloadUrl);

        if (response == null)
        {
            logger.LogError("Failed to download blob content '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        return new BlobInfo(name, response);
    }
}