using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using UnrealEngine.Runtime;
using WukongMp.Api;

namespace WukongMp.Sdk.Api.Implementation;

/// API for referencing files related to a mod, such as save files.
internal sealed class WukongFileApi : IWukongFileApi
{
    public string GetModDirectory<T>() where T : ModBase
    {
        if (LaunchParameters.Instance.ModFolderOverride == null)
            throw new NotImplementedException("GetModDirectory is not implemented for non-override mod folder. Please specify ModFolderOverride in launch parameters.");

        var assemblyLocation = typeof(T).Assembly.Location;
        var folderName = Path.GetFileName(Path.GetDirectoryName(assemblyLocation));

        return FPaths.Combine(LaunchParameters.Instance.ModFolderOverride, folderName);
    }
}