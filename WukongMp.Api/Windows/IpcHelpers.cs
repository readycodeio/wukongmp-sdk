using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WukongMp.Api.Windows;

// TODO: Move to common API
internal static class IpcHelpers
{
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

            throw new Win32Exception(error);
        }

        if (size > buffer.Capacity)
        {
            buffer = new StringBuilder((int)size);
            GetEnvironmentVariable(variable, buffer, size);
        }

        return buffer.ToString();
    }

    private static readonly HashSet<string> RedactedKeys = ["JWT_TOKEN"];

    public static Dictionary<string, string> ReadAndDeleteIpcHandshakeFile()
    {
        Logging.LogInformation("Resolving the handshake file path");
        var tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ReadyM.Launcher");
        var filePath = Path.Combine(tempDir, "wukong_handshake.env");

        if (!File.Exists(filePath))
        {
            Logging.LogError("Handshake file not found at {Path}. Launch the game from the ReadyM Launcher.", filePath);
            return [];
        }

        Logging.LogInformation("Reading handshake file: {FilePath}", filePath);
        var lines = File.ReadAllLines(filePath);
        var data = new Dictionary<string, string>();

        // format is .env KEY=VALUE
        var regex = new Regex(@"^(?<key>[^=]+)=(?<value>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                var key = match.Groups["key"].Value.Trim();
                var value = match.Groups["value"].Value.Trim();
                data[key] = value;

                if (RedactedKeys.Contains(key))
                {
                    Logging.LogInformation("Parsed {Key}=<redacted>", key);
                }
                else
                {
                    Logging.LogInformation("Parsed {Key}={Value}", key, value);
                }
            }
            else
            {
                Logging.LogError("Failed to parse line: {Line}", line);
            }
        }

        // delete the file after reading
        try
        {
            File.Delete(filePath);
            Logging.LogInformation("Deleted handshake file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Logging.LogException(ex);
        }

        return data;
    }
}