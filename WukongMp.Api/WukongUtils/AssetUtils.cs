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
        UAssetDataArray assetsInFolder = UGSE_AssetUtilFuncLib.GetAssetsInFolder(new FName(path), bRecursive: true);
        if (assetsInFolder == null)
        {
            return;
        }

        int i = 0;
        foreach (FAssetData item6 in assetsInFolder.AssetDataArr)
        {
            Logging.LogInformation("Asset {Id} path : {Name}", i++, item6.GetFullName());
        }
    }

    public static void PlayBossDefeatedSound()
    {
        var playUiSound = AccessTools.Method("B1UI.Script.GSUI.Util.GSUIAudioUtil:PlayUISound");
        playUiSound.Invoke(null, ["EVT_ui_kill_jisha_manjingtou"]);
    }
}