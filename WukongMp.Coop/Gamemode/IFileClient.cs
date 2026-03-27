using FileInfo = WukongMp.Api.Https.FileInfo;

namespace WukongMp.Coop.Gamemode;

public interface IFileClient
{
    Task<bool> UploadFileAsync(FileInfo file, CancellationToken ct = default);
    Task<FileInfo?> DownloadFileAsync(string name, CancellationToken ct = default);
}