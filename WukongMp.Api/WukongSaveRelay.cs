using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WukongMp.Api.Configuration;
using WukongMp.Api.Https;

namespace WukongMp.Api;

internal class WukongSaveRelay(IBlobClient blobClient, ILogger logger) : IWukongSaveRelay
{
    public Task<bool> UploadWorldSaveAsync(byte[] content, CancellationToken ct = default)
        => UploadBlobAsync(Constants.CoopWorldArchiveName, content, ct);

    public Task<BlobInfo?> DownloadWorldSaveAsync(CancellationToken ct = default)
        => DownloadBlobAsync(Constants.CoopWorldArchiveName, ct);

    public Task<bool> UploadPlayerSaveAsync(byte[] content, CancellationToken ct = default)
        => UploadBlobAsync(PlayerSaveName, content, ct);

    public Task<BlobInfo?> DownloadPlayerSaveAsync(CancellationToken ct = default)
        => DownloadBlobAsync(PlayerSaveName, ct);

    private static string PlayerSaveName => $"player_{LaunchParameters.Instance.UserGuid:N}.sav";
    
    private Task<bool> UploadBlobAsync(string name, byte[] content, CancellationToken ct = default)
    {
        try
        {
            return blobClient.UploadBlobAsync(new BlobInfo(name, content), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload blob: {BlobName}", name);
            throw new OperationCanceledException("Failed to upload blob", ex);
        }
    }

    private Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        try
        {
            return blobClient.DownloadBlobAsync(name, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download blob: {BlobName}", name);
            throw new OperationCanceledException("Failed to download blob", ex);
        }
    }
}