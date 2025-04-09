using System.Text.RegularExpressions;
using UnrealEngine.Engine;

namespace WukongApi;

public class CmdLineParams
{
    private static CmdLineParams? _instance;

    public static CmdLineParams Instance => _instance ??= new CmdLineParams();
    public bool ShouldEnableMultiplayer => ServerIp is not null && ServerPort is not null;

    public GameMode? MatchmakingMode { get; }
    public string? ModFolderOverride { get; }
    public int? PlayersPerTeam { get; private set; }
    public string? ServerIp { get; }
    public int? ServerPort { get; }

    private CmdLineParams()
    {
        var cmd = USystemLibrary.GetCommandLine();

        Logging.LogDebug("Command line: {Args}", cmd);

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

        // check for custom mod folder
        const string modFolderPattern = """[a-zA-Z]:\\(?:[^<>:"/\\|?*]+\\)*[^<>:"/\\|?*]*""";
        var pathMatch = Regex.Match(cmd, $"""-mod_folder "?({modFolderPattern})"?""");

        if (pathMatch.Success)
        {
            ModFolderOverride = pathMatch.Groups[1].Value;
            Logging.LogDebug("Mod folder: {Folder}", ModFolderOverride);
        }

        // this can be either a private match (-room_name "name") or a quick match (-quick_match 1/3/5)
        var quickMatchMatch = Regex.Match(cmd, @"-quick_match (\d)");
        if (quickMatchMatch.Success)
        {
            // quick match
            var rounds = int.Parse(quickMatchMatch.Groups[1].Value);
            PlayersPerTeam = rounds;
            MatchmakingMode = GameMode.XvX;
        }
        else
        {
            MatchmakingMode = GameMode.Private;
        }
    }
}