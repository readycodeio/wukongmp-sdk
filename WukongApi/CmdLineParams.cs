using System.Text.RegularExpressions;
using UnrealEngine.Engine;

namespace WukongApi;

public class CmdLineParams
{
    private static CmdLineParams? _instance;

    public static CmdLineParams Instance => _instance ??= new CmdLineParams();
    public bool ShouldEnableMultiplayer => AccessToken is not null;

    public GameMode? MatchmakingMode { get; }
    public string? ModFolderOverride { get; }
    public string? RoomName { get; private set; }
    public int? PlayersPerTeam { get; private set; }
    public string? AccessToken { get; }

    private CmdLineParams()
    {
        var cmd = USystemLibrary.GetCommandLine();

        Logging.LogDebug("Command line: {Args}", cmd);

        var tokenMatch = Regex.Match(cmd, $"""-access_token "?({Constants.JsonCompactSerializationRegex})"?""");

        if (tokenMatch.Success)
        {
            AccessToken = tokenMatch.Groups[1].Value;
        }
        else
        {
            Logging.LogError("Access token not provided. Launch the game from the ReadyM Launcher.");
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

        var roomNameMatch = Regex.Match(cmd, """-room_name "([a-zA-Z0-9_\- ]+)"|-room_name ([a-zA-Z0-9_\-]+)""");
        if (roomNameMatch.Success)
        {
            // private match
            RoomName = roomNameMatch.Groups[1].Success ? roomNameMatch.Groups[1].Value : roomNameMatch.Groups[2].Value;
            MatchmakingMode = GameMode.Private;
        }
        else
        {
            var quickMatchMatch = Regex.Match(cmd, @"-quick_match (\d)");
            if (quickMatchMatch.Success)
            {
                // quick match
                var rounds = int.Parse(quickMatchMatch.Groups[1].Value);
                MatchmakingMode = GameMode.XvX;
                PlayersPerTeam = rounds;
            }
            else
            {
                Logging.LogError("Room name not provided. Launch the game from the ReadyM Launcher.");
                return;
            }
        }
    }
}