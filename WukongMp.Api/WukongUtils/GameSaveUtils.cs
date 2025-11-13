using System.Reflection;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils;

public static class GameSaveUtils
{
    public static string GetModsDirectory()
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

    // TODO: We need a universal way to get mod DLL name. We pass Assembly for now, but it's a hack.
    public static string GetSaveFileFullName(Assembly modAssembly, string slotName)
    {
        slotName += ".sav";
        return FPaths.Combine(GetModDirectory(modAssembly), slotName);
    }
}