using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using UnrealEngine.Runtime;
using WukongMp.Api;

namespace WukongMp.Sdk.Api.Implementation;

/// API for referencing files related to a mod, such as save files.
internal sealed class WukongFileApi(ILogger logger) : IWukongFileApi
{
    public string GetSaveFileFullName<T>(string slotName) where T : ModBase
    {
        slotName += ".sav";
        var path = FPaths.Combine(GetModDirectory(typeof(T)), slotName);

        logger.LogDebug("Redirecting save file to {Path}", path);
        return path;
    }

    private static string GetModDirectory(Type modType)
    {
        if (LaunchParameters.Instance.ModFolderOverride == null)
            throw new NotImplementedException("GetModDirectory is not implemented for non-override mod folder. Please specify ModFolderOverride in launch parameters.");

        var assemblyLocation = modType.Assembly.Location;
        var folderName = Path.GetFileName(Path.GetDirectoryName(assemblyLocation));

        return FPaths.Combine(LaunchParameters.Instance.ModFolderOverride, folderName);
    }
}