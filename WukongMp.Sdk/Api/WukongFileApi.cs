using System;
using Microsoft.Extensions.Logging;
using UnrealEngine.Runtime;
using WukongMp.Api;

namespace WukongMp.Sdk.Api;

public static class WukongFileApi
{
    private static string GetModDirectory(ModBase mod)
    {
        if (LaunchParameters.Instance.ModFolderOverride != null)
        {
            return FPaths.Combine(LaunchParameters.Instance.ModFolderOverride, mod.Name);
        }

        throw new NotImplementedException("GetModDirectory is not implemented for non-override mod folder. Please specify ModFolderOverride in launch parameters.");
    }

    public static string GetSaveFileFullName(ModBase mod, string slotName)
    {
        slotName += ".sav";
        var path = FPaths.Combine(GetModDirectory(mod), slotName);

        DI.Instance.Logger.LogDebug("Redirecting save file to {Path}", path);
        return path;
    }
}