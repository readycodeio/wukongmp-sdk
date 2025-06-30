using b1;
using b1.BGW;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils;

public static class MagicallyChangeUtils
{
    public static void TriggerMagicallyChange(BGUCharacterCS pawn, string configAssetPath, int skillID, int recoverSkillID)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var world = GameUtils.GetWorld();
            BGWDataAsset_MagicallyChangeConfig config = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<BGWDataAsset_MagicallyChangeConfig>(configAssetPath, ELoadResourceType.SyncLoadAndCache);
            if (config == null)
            {
                Logging.LogError("Failed to load MagicallyChangeConfig from path: {Path}", configAssetPath);
                return;
            }
            Logging.LogDebug("Received trigger magically change for character {Nickname} with config {ConfigAssetPath}, skillID {SkillID}, recoverSkillID {RecoverSkillID}", pawn.GetName(), configAssetPath, skillID, recoverSkillID);
            BUC_MagicallyChangeData magicallyChangeData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_MagicallyChangeData, BUC_MagicallyChangeData>(pawn);
            magicallyChangeData.bIsPendingCast = true;
            magicallyChangeData.bIsPendingReset = false;
            magicallyChangeData.PendingConfig = config;
            magicallyChangeData.MagicallyChangeSkillID = skillID;
            magicallyChangeData.RecoverSkillID = recoverSkillID;

        }, nameof(TriggerMagicallyChange));
    }
}
