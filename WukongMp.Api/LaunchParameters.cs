using System;
using System.Collections.Generic;
using System.IO;
using ReadyM.Api.Multiplayer.Client;
using WukongMp.Api.Compat;
using WukongMp.Api.Configuration;
using WukongMp.Api.Windows;

namespace WukongMp.Api;

internal sealed class LaunchParameters
{
    private static LaunchParameters? _instance;
    public static LaunchParameters Instance => _instance ??= new LaunchParameters();

    public bool Valid => ServerIp is not null
                         && ServerPort is not null
                         && (Ticket != default || LevelId != null || ServerId != null); // self-hosted, pvp, coop

    public string? ModFolderOverride { get; }
    public string? GameMode { get; }
    public string? ServerIp { get; }
    public int? ServerPort { get; }
    public int? ServerId { get; }
    public Guid UserGuid { get; }
    public string? ApiBaseUrl { get; }
    public string? JwtToken { get; } // TODO: Internalize the co-op save client and use this
    public ConnectionTicket Ticket { get; }
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

    private readonly Dictionary<string, string> _allParameters;

    public string GetParameterOrDefault(string key, string defaultValue)
    {
        return _allParameters.GetValueOrDefault(key, defaultValue);
    }

    private LaunchParameters()
    {
        var data = _allParameters = IpcHelpers.ReadAndDeleteIpcHandshakeFile();

        // Hosted: Game mode
        GameMode = data.GetValueOrDefault("GAME_MODE").ToLowerInvariant();

        // CO-OP: API base URL
        ApiBaseUrl = data.GetValueOrDefault("API_BASE_URL");

        // JWT token 
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
        
        // BOTH: single use connection ticket
        var ticketString = data.GetValueOrDefault("TICKET");

        if (ConnectionTicket.TryParse(ticketString, out var ticket))
        {
            Ticket = ticket.Value;
            Logging.LogDebug("Ticket: {Guid}", Ticket);
        }
        else
        {
            Logging.LogError("Invalid Ticket format: {Guid}", ticketString);
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