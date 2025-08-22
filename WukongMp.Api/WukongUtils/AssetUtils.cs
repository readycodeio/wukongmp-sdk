using b1;
using b1.BGW;
using BtlB1;
using HarmonyLib;
using System.Collections.Generic;
using UnrealEngine.AssetRegistry;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils;

public static class AssetUtils
{
    public static UBGWDataAsset? GetFxAssetByResId(UObject context, IList<FPlayFXByResID> fXs, int targetResId, int ownerResId)
    {
        var text = "";
        foreach (var fx in fXs)
        {
            if (fx.ResID == targetResId)
            {
                text = fx.FXPathByDBC;
                break;
            }

            if (fx.ResID == ownerResId)
            {
                text = fx.FXPathByDBC;
            }
        }

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return BGW_PreloadAssetMgr.Get(context).TryGetCachedResourceObj<UBGWDataAsset>(text, ELoadResourceType.AsyncLoadAndCache);
    }

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

    public static void PlayBossDefeatedSound()
    {
        var playUiSound = AccessTools.Method("B1UI.Script.GSUI.Util.GSUIAudioUtil:PlayUISound");
        playUiSound.Invoke(null, ["EVT_ui_kill_jisha_manjingtou"]);
    }
}