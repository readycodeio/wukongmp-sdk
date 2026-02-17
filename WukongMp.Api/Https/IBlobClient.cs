using System.Threading;
using System.Threading.Tasks;

namespace WukongMp.Api.Https;

public interface IBlobClient
{
    Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default);
    Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default);
}