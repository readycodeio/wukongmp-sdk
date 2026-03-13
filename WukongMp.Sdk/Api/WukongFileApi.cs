using System;
using Microsoft.Extensions.Logging;
using UnrealEngine.Runtime;
using WukongMp.Api;

namespace WukongMp.Sdk.Api;

/// API for referencing files related to a mod, such as save files.
public sealed class WukongFileApi(ILogger logger)
{
    private static string GetModDirectory(ModBase mod)
    {
        if (LaunchParameters.Instance.ModFolderOverride == null)
            throw new NotImplementedException("GetModDirectory is not implemented for non-override mod folder. Please specify ModFolderOverride in launch parameters.");

        return FPaths.Combine(LaunchParameters.Instance.ModFolderOverride, mod.Name);
    }

    public string GetSaveFileFullName(ModBase mod, string slotName)
    {
        slotName += ".sav";
        var path = FPaths.Combine(GetModDirectory(mod), slotName);

        logger.LogDebug("Redirecting save file to {Path}", path);
        return path;
    }
}