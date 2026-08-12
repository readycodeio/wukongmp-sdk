using System.Reflection;
using Microsoft.Extensions.Logging;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils;

internal static class GameSaveUtils
{
    internal static string GetModsDirectory()
    {
        if (LaunchParameters.Instance.ModFolderOverride != null)
        {
            return LaunchParameters.Instance.ModFolderOverride;
        }

        return FPaths.Combine(FPaths.ProjectDir, "Binaries", "Win64", "CSharpLoader", "Mods");
    }

    private static string GetModDirectory(Assembly modAssembly)
    {
        var modName = modAssembly.GetName().Name;

        if (LaunchParameters.Instance.ModFolderOverride != null)
        {
            return FPaths.Combine(LaunchParameters.Instance.ModFolderOverride, modName);
        }

        return FPaths.Combine(FPaths.ProjectDir, "Binaries", "Win64", "CSharpLoader", "Mods", modName);
    }

    internal static string GetSaveFileFullName(Assembly modAssembly, string slotName)
    {
        slotName += ".sav";
        var path = FPaths.Combine(GetModDirectory(modAssembly), slotName);

        DI.Instance.Logger.LogDebug("Redirecting save file to {Path}", path);
        return path;
    }
}