using System;
using System.IO;
using WukongMp.Api.Compat;
using WukongMp.Api.Configuration;
using WukongMp.Api.Windows;

namespace WukongMp.Api;

public class LaunchParameters
{
    private static LaunchParameters? _instance;
    public static LaunchParameters Instance => _instance ??= new LaunchParameters();

    public bool ShouldEnableMultiplayer => ServerIp is not null && ServerPort is not null;

    public string? ModFolderOverride { get; }
    public string? ServerIp { get; }
    public int? ServerPort { get; }
    public int? ServerId { get; }
    public Guid UserGuid { get; } = Guid.Empty;
    public string? ApiBaseUrl { get; }
    public string? JwtToken { get; }
    public string Nickname { get; } = "Player";
    public int LevelId { get; }

    public string? ShimDbName { get; }
    public string? ShimDbDir { get; }

    public bool RecordShimOnStart
        => RecordShimName != null;

    public string? RecordShimName { get; }
    public string? RecordShimFile { get; }

    public bool PlayShimOnStart
        => PlayShimName != null;

    public string? PlayShimName { get; }
    public string? PlayShimFile { get; }

    private LaunchParameters()
    {
        var data = IpcHelpers.ReadAndDeleteIpcHandshakeFile();

        if (Constants.IsCoop)
        {
            // REQUIRED: API base URL
            ApiBaseUrl = data.GetValueOrDefault("API_BASE_URL");
            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                Logging.LogError("API base URL not provided, launch the game from the ReadyM Launcher.");
                return;
            }
        }

        if (Constants.IsCoop)
        {
            // REQUIRED: JWT token
            JwtToken = data.GetValueOrDefault("JWT_TOKEN");
            if (string.IsNullOrWhiteSpace(JwtToken))
            {
                Logging.LogError("Authorization token not provided, launch the game from the ReadyM Launcher.");
                return;
            }
        }

        // REQUIRED: user GUID
        var guidString = data.GetValueOrDefault("PLAYER_ID");
        if (string.IsNullOrWhiteSpace(guidString))
        {
            Logging.LogError("User ID not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        if (Guid.TryParse(guidString, out var guid))
        {
            UserGuid = guid;
            Logging.LogDebug("User ID: {Guid}", UserGuid);
        }
        else
        {
            Logging.LogError("Invalid ID format: {Guid}", guidString);
            return;
        }

        if (Constants.IsCoop)
        {
            // REQUIRED: server ID
            var serverIdString = data.GetValueOrDefault("SERVER_ID");
            if (string.IsNullOrWhiteSpace(serverIdString))
            {
                Logging.LogError("Server ID not provided, launch the game from the ReadyM Launcher.");
                return;
            }

            ServerId = int.Parse(serverIdString);
        }

        // REQUIRED: server IP and port number
        ServerIp = data.GetValueOrDefault("SERVER_IP");
        var serverPort = data.GetValueOrDefault("SERVER_PORT");

        if (string.IsNullOrWhiteSpace(ServerIp) || string.IsNullOrWhiteSpace(serverPort))
        {
            Logging.LogError("Server IP or port not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        ServerPort = int.Parse(serverPort);

        // REQUIRED: user nickname
        Nickname = data.GetValueOrDefault("NICKNAME") ?? "";
        if (string.IsNullOrWhiteSpace(Nickname))
        {
            Logging.LogError("Nickname not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        if (Constants.IsPvP)
        {
            // REQUIRED: Level ID
            var level = data.GetValueOrDefault("LEVEL_ID");
            if (!string.IsNullOrWhiteSpace(level))
            {
                LevelId = int.Parse(level);
                Logging.LogDebug("Level ID: {LevelId}", LevelId);
            }
            else
            {
                Logging.LogError("Level ID not provided, launch the game from the ReadyM Launcher.");
                return;
            }
        }

        // OPTIONAL: custom mod folder
        var modFolder = data.GetValueOrDefault("MOD_FOLDER");

        if (!string.IsNullOrWhiteSpace(modFolder))
        {
            ModFolderOverride = modFolder;
            Logging.LogDebug("Mod folder: {Folder}", ModFolderOverride);
        }

        // OPTIONAL: record shim test
        var shimDb = data.GetValueOrDefault("SHIM_DB");

        if (!string.IsNullOrWhiteSpace(shimDb))
        {
            ShimDbName = shimDb;
            Logging.LogDebug("Shim DB: {ShimDbName}", ShimDbName);
        }
        else
        {
            ShimDbName = "Default";
            Logging.LogDebug("Shim DB not provided, using: Default");
        }

        ShimDbDir = Path.GetFullPath($"{Constants.ShimFolder}/{ShimDbName}");

        // OPTIONAL: record shim test
        var recordShim = data.GetValueOrDefault("RECORD_SHIM");

        if (!string.IsNullOrWhiteSpace(recordShim))
        {
            RecordShimName = recordShim;
            RecordShimFile = Path.GetFullPath($"{ShimDbDir}/{RecordShimName}.shim");
            Logging.LogDebug("Record Shim: {RecordShimFile}", RecordShimFile);
        }

        // OPTIONAL: play shim test
        var playShim = data.GetValueOrDefault("PLAY_SHIM");
        if (!string.IsNullOrWhiteSpace(playShim))
        {
            PlayShimName = playShim;
            PlayShimFile = Path.GetFullPath($"{ShimDbDir}/{PlayShimName}.shim");
            Logging.LogDebug("Play Shim: {PlayShimFile}", PlayShimFile);
        }
    }
}