using System;
using System.IO;
using WukongMp.Api.Compat;
using WukongMp.Api.Configuration;
using WukongMp.Api.Windows;

namespace WukongMp.Api;

internal class LaunchParameters
{
    private static LaunchParameters? _instance;
    public static LaunchParameters Instance => _instance ??= new LaunchParameters();

    public bool Valid => ServerIp is not null
                         && ServerPort is not null
                         && UserGuid != Guid.Empty;

    public bool ValidForCoOp => Valid && JwtToken is not null
                                      && ApiBaseUrl is not null
                                      && ServerId is not null;

    public bool ValidForPvP => LevelId is not null;

    public string? ModFolderOverride { get; }
    public string? ServerIp { get; }
    public int? ServerPort { get; }
    public int? ServerId { get; }
    public Guid UserGuid { get; } = Guid.Empty;
    public string? ApiBaseUrl { get; }
    public string? JwtToken { get; }
    public string Nickname { get; }
    public int Region { get; } = -1;
    public int? LevelId { get; set; }

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

        // CO-OP: API base URL
        ApiBaseUrl = data.GetValueOrDefault("API_BASE_URL");

        // CO-OP: JWT token
        JwtToken = data.GetValueOrDefault("JWT_TOKEN");

        // BOTH: user GUID
        var guidString = data.GetValueOrDefault("PLAYER_ID");

        if (Guid.TryParse(guidString, out var guid))
        {
            UserGuid = guid;
            Logging.LogDebug("User ID: {Guid}", UserGuid);
        }
        else
        {
            Logging.LogError("Invalid ID format: {Guid}", guidString);
        }

        // CO-OP: server ID
        var serverIdString = data.GetValueOrDefault("SERVER_ID");
        if (!string.IsNullOrWhiteSpace(serverIdString) && int.TryParse(serverIdString, out var id))
        {
            ServerId = id;
        }

        // BOTH: server IP and port number
        ServerIp = data.GetValueOrDefault("SERVER_IP");

        var serverPort = data.GetValueOrDefault("SERVER_PORT", "");
        if (int.TryParse(serverPort, out var port))
        {
            ServerPort = port;
        }

        // BOTH: user nickname
        Nickname = data.GetValueOrDefault("NICKNAME");

        // BOTH: server region
        var region = data.GetValueOrDefault("REGION", "");
        if (int.TryParse(region, out var regionId))
        {
            Region = regionId;
        }

        // PvP: Level ID
        var level = data.GetValueOrDefault("LEVEL_ID");
        if (!string.IsNullOrWhiteSpace(level) && int.TryParse(level, out var levelId))
        {
            LevelId = levelId;
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