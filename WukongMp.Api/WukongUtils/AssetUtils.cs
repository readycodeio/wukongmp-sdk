using b1;
using HarmonyLib;
using UnrealEngine.AssetRegistry;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils;

internal static class AssetUtils
{
    public static void ListAssetsInFolder(string path)
    {
        var assetsInFolder = UGSE_AssetUtilFuncLib.GetAssetsInFolder(new FName(path), bRecursive: true);
        if (assetsInFolder == null)
        {
            return;
        }

        var i = 0;
        foreach (var item in assetsInFolder.AssetDataArr)
        {
            Logging.LogDebug("Asset {Id} path : {Name}", i++, item.GetFullName());
        }
    }


}