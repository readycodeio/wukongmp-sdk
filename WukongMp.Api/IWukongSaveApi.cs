using System.Threading;
using System.Threading.Tasks;
using ReadyM.Api.Saves;
using WukongMp.Api.Https;

namespace WukongMp.Api;

/// <summary>
/// API for managing world and player saves in Wukong Multiplayer.
/// Supports co-op style operations where each player has their own save file, and there's a shared world save file for all players.
/// </summary>
public interface IWukongSaveApi
{
    /// <summary>
    /// Uploads the world save file to the server.
    /// This operation always overwrites the existing world save file on the server, if any.
    /// At this point, the server is expected to keep only the most recent world save file for all players.
    /// </summary>
    /// <param name="content">The content of the world save file to upload. </param>
    /// <param name="ct">Cancellation token to cancel the upload operation. </param>
    /// <returns> <see langword="true"/> if the save was uploaded successfully, <see langword="false"/> otherwise. </returns>
    Task<bool> UploadWorldSaveAsync(byte[] content, CancellationToken ct = default);

    /// <summary>
    /// Downloads the world save file from the server.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the download operation. </param>
    /// <returns> The content of the world save file if the download was successful, <see langword="null"/> otherwise. </returns>
    Task<FileInfo?> DownloadWorldSaveAsync(CancellationToken ct = default);

    /// <summary>
    /// Uploads the player's save file to the server.
    /// This operation always overwrites the existing save file on the server, if any.
    /// At this point, the server is expected to keep only the most recent save file for each player.
    /// </summary>
    /// <param name="content">The content of the save file to upload. </param>
    /// <param name="ct">Cancellation token to cancel the upload operation. </param>
    /// <returns> <see langword="true"/> if the save was uploaded successfully, <see langword="false"/> otherwise. </returns>
    Task<bool> UploadPlayerSaveAsync(byte[] content, CancellationToken ct = default);

    /// <summary>
    /// Downloads the player's save file from the server.
    /// This operation is expected to return the most recent save file for the player, if any.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the download operation. </param>
    /// <returns> The content of the player's save file if the download was successful, <see langword="null"/> otherwise. </returns>
    Task<FileInfo?> DownloadPlayerSaveAsync(CancellationToken ct = default);
}