using System.Threading;
using System.Threading.Tasks;
using WukongMp.Api.Https;

namespace WukongMp.Api;

public interface IWukongSaveRelay
{
    Task<bool> UploadWorldSaveAsync(byte[] content, CancellationToken ct = default);
    Task<BlobInfo?> DownloadWorldSaveAsync(CancellationToken ct = default);
    Task<bool> UploadPlayerSaveAsync(byte[] content, CancellationToken ct = default);
    Task<BlobInfo?> DownloadPlayerSaveAsync(CancellationToken ct = default);
}