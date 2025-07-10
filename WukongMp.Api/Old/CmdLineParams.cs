using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Old;

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
    public int LevelId { get; }

    private CmdLineParams()
    {
        // REQUIRED: user GUID
        var guidString = GetEnvironmentVariable("WUKONGMP_ID");
        if (string.IsNullOrWhiteSpace(guidString))
        {
            Logging.LogError("GUID not provided, launch the game from the ReadyM Launcher.");
            return;
        }

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

        // REQUIRED: server IP and port number
        ServerIp = GetEnvironmentVariable("WUKONGMP_SERVER_IP");
        var serverPort = GetEnvironmentVariable("WUKONGMP_SERVER_PORT");

        if (string.IsNullOrWhiteSpace(ServerIp) || string.IsNullOrWhiteSpace(serverPort))
        {
            Logging.LogError("Server IP or port not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        ServerPort = int.Parse(serverPort);

        // REQUIRED: user nickname
        Nickname = GetEnvironmentVariable("WUKONGMP_NICKNAME") ?? "";
        if (string.IsNullOrWhiteSpace(Nickname))
        {
            Logging.LogError("Nickname not provided, launch the game from the ReadyM Launcher.");
            return;
        }

        if (!Constants.IsCoop)
        {
            // REQUIRED: Level ID
            var level = Environment.GetEnvironmentVariable("WUKONGMP_LEVEL_ID");
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
        var modFolder = GetEnvironmentVariable("WUKONGMP_MOD_FOLDER");

        if (!string.IsNullOrWhiteSpace(modFolder))
        {
            ModFolderOverride = modFolder;
            Logging.LogDebug("Mod folder: {Folder}", ModFolderOverride);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetEnvironmentVariable(string lpName, StringBuilder lpBuffer, uint nSize);

    private static string? GetEnvironmentVariable(string variable)
    {
        const int initialSize = 512;
        StringBuilder buffer = new(initialSize);

        var size = GetEnvironmentVariable(variable, buffer, (uint)buffer.Capacity);
        if (size == 0)
        {
            var error = Marshal.GetLastWin32Error();
            if (error == 203) // ERROR_ENVVAR_NOT_FOUND
                return null;
            if (error == 0 && buffer.Length == 0)
                return null;

            throw new System.ComponentModel.Win32Exception(error);
        }

        if (size > buffer.Capacity)
        {
            buffer = new StringBuilder((int)size);
            GetEnvironmentVariable(variable, buffer, size);
        }

        return buffer.ToString();
    }
}