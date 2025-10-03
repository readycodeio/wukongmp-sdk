using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client.Blobs;
using ReadyM.Relay.Client.Blobs;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Https;

public class HttpBlobClient(ILogger logger) : IBlobClient
{
    private enum FileType
    {
        WorldSave,
        PlayerSave
    }

    public async Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default)
    {
        var client = new BouncyCastleHttpsClient(logger);
        var serverId = LaunchParameters.Instance.ServerId!.Value;
        var kind = blob.Name == Constants.CoopWorldArchiveName ? FileType.WorldSave : FileType.PlayerSave;

        Guid? userGuid = null;
        if (kind == FileType.PlayerSave)
        {
            // name is like "player_<userGuid>.sav"
            var parts = blob.Name.Split('_', '.');
            if (parts.Length == 3 && Guid.TryParse(parts[1], out var parsedGuid))
            {
                userGuid = parsedGuid;
            }
        }

        var query = $"?kind={kind}&userGuid={userGuid}&serverId={serverId}";
        var url = new Uri($"{LaunchParameters.Instance.ApiBaseUrl}/api/server/{serverId}/files/upload-sas{query}");
        var uploadUrl = await client.GetAsync<string>(url, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {LaunchParameters.Instance.JwtToken}" }
        }, ct);

        if (uploadUrl is not null)
        {
            // compress the blob with GZIP
            using var stream = new System.IO.MemoryStream();
            using var gzip = new GZipStream(stream, CompressionLevel.Optimal, true);
            await gzip.WriteAsync(blob.Content, 0, blob.Content.Length, ct);
            byte[] gzippedContent = stream.ToArray();
            
            var md5checksum = MD5.Create().ComputeHash(gzippedContent);

            // this is a SAS URL for Azure Blob Storage
            var uploadUri = new Uri(uploadUrl);
            
            // https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id#request-headers-all-blob-types
            var headers = new Dictionary<string, string>
            {
                { "x-ms-blob-type", "BlockBlob" },
                { "x-ms-version", "2025-07-05" },
                { "Content-Encoding", "gzip" },
                { "x-ms-blob-content-encoding", "gzip" },
                { "Content-MD5", Convert.ToBase64String(md5checksum) }
            };
            var status = await client.PutBytesAsync(uploadUri, headers, gzippedContent, ct);
            return status is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous;
        }

        logger.LogError("Failed to get upload URL for blob '{BlobName}' for server {ServerId}", blob.Name, serverId);
        return false;
    }

    public async Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        var serverId = LaunchParameters.Instance.ServerId!.Value;
        var nameEscaped = Uri.EscapeDataString(name);

        var client = new BouncyCastleHttpsClient(logger);

        // Download
        var linkUrl = new Uri($"{LaunchParameters.Instance.ApiBaseUrl}/api/server/{serverId}/files/{nameEscaped}");
        var downloadResponse = await client.GetAsync<DownloadServerFileResponse>(linkUrl, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {LaunchParameters.Instance.JwtToken}" }
        }, ct);

        if (string.IsNullOrWhiteSpace(downloadResponse?.DownloadUrl))
        {
            logger.LogWarning("Failed to get download URL for blob '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        var downloadUrl = new Uri(downloadResponse!.DownloadUrl);
        var response = await client.GetBytesAsync(downloadUrl, ct: ct);

        if (response == null)
        {
            logger.LogError("Failed to download blob content '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        return new BlobInfo(name, response);
    }
}