using System.Threading;
using System.Threading.Tasks;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;

namespace WukongMp.Api;

public class WukongSaveRelay(IRelayClient relayClient)
{
    public Task<bool> UploadWorldSaveAsync(byte[] content, CancellationToken ct = default)
        => relayClient.UploadBlobAsync(new BlobInfo(Constants.CoopWorldArchiveName, content), ct);

    public Task<BlobInfo?> DownloadWorldSaveAsync(CancellationToken ct = default) 
        => relayClient.DownloadBlobAsync(Constants.CoopWorldArchiveName, ct);

    public Task<bool> UploadPlayerSaveAsync(byte[] content, CancellationToken ct = default)
        => relayClient.UploadBlobAsync(new BlobInfo(PlayerSaveName, content), ct);

    public Task<BlobInfo?> DownloadPlayerSaveAsync(CancellationToken ct = default) 
        => relayClient.DownloadBlobAsync(PlayerSaveName, ct);

    private static string PlayerSaveName => $"player_{CmdLineParams.Instance.UserGuid:N}.sav";
}