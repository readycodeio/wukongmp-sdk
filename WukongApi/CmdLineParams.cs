using System;
using System.Text.RegularExpressions;
using UnrealEngine.Engine;

namespace WukongApi;

public class CmdLineParams
{
    private static CmdLineParams? _instance;

    public static CmdLineParams Instance => _instance ??= new CmdLineParams();
    public bool ShouldEnableMultiplayer => ServerIp is not null && ServerPort is not null;

    public string? ModFolderOverride { get; }
    public string? ServerIp { get; }
    public int? ServerPort { get; }
    public Guid UserGuid { get; } = Guid.Empty;
    public string Nickname { get; } = "Player";

    // room creation options
    public RoomCreationOptions? RoomCreationOptions { get; }

    private CmdLineParams()
    {
        var cmd = USystemLibrary.GetCommandLine();

        Logging.LogDebug("Command line: {Args}", cmd);

        // REQUIRED: user GUID
        var idMatch = Regex.Match(cmd, """-id "?([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})"?""");
        if (idMatch.Success)
        {
            var guidString = idMatch.Groups[1].Value;
            if (Guid.TryParse(guidString, out var guid))
            {
                UserGuid = guid;
                Logging.LogDebug("User GUID: {Guid}", UserGuid);
            }
            else
            {
                Logging.LogError("Invalid GUID format: {Guid}", guidString);
                return;
            }
        }
        else
        {
            Logging.LogError("GUID not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        // REQUIRED: server IP and port number
        var serverMatch = Regex.Match(cmd, @"-serverIp ""?([0-9\.]+)""? -serverPort ""?(\d+)""?");

        if (serverMatch.Success)
        {
            ServerIp = serverMatch.Groups[1].Value;
            ServerPort = int.Parse(serverMatch.Groups[2].Value);
            Logging.LogDebug("Server IP: {Ip}, Port: {Port}", ServerIp, ServerPort);
        }
        else
        {
            Logging.LogError("Connection info not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        // REQUIRED: user nickname
        var nicknameMatch = Regex.Match(cmd, """-nickname "?(\w+)"?""");
        if (nicknameMatch.Success)
        {
            Nickname = nicknameMatch.Groups[1].Value;
            Logging.LogDebug("Nickname: {Nickname}", Nickname);
        }
        else
        {
            Logging.LogError("Nickname not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        // OPTIONAL: custom mod folder
        const string modFolderPattern = """[a-zA-Z]:\\(?:[^<>:"/\\|?*]+\\)*[^<>:"/\\|?*]*""";
        var pathMatch = Regex.Match(cmd, $"""-mod_folder "?({modFolderPattern})"?""");

        if (pathMatch.Success)
        {
            ModFolderOverride = pathMatch.Groups[1].Value;
            Logging.LogDebug("Mod folder: {Folder}", ModFolderOverride);
        }

        // OPTIONAL: room creation params
        var roomMatch = Regex.Match(cmd, """-rounds ""?(\d+)""? -gourd ""?(\w+)""? -immobilize ""?(\w+)""? -phantomRush ""?(\w+)""? -ng ""?(\d+)""?""");
        if (roomMatch.Success)
        {
            var rounds = int.Parse(roomMatch.Groups[1].Value);
            var flask = bool.Parse(roomMatch.Groups[2].Value.ToLowerInvariant());
            var immobilize = bool.Parse(roomMatch.Groups[3].Value.ToLowerInvariant());
            var phantomRush = bool.Parse(roomMatch.Groups[4].Value.ToLowerInvariant());
            var ngPlus = int.Parse(roomMatch.Groups[5].Value);

            RoomCreationOptions = new RoomCreationOptions(rounds, ngPlus, flask, immobilize, phantomRush);

            Logging.LogDebug("Room creation options set");
        }
    }
}