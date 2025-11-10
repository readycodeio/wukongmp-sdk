using b1;
using b1.BGW;

namespace WukongMp.Api.WukongUtils;

public static class MagicallyChangeUtils
{
    public static void TriggerMagicallyChange(BGUCharacterCS pawn, string configAssetPath, int skillID, int recoverSkillID, int curVigorSkillID, ECastReason_MagicallyChange castReason = ECastReason_MagicallyChange.VigorSkill)
    {
        var world = GameUtils.GetWorld();
        UBGWDataAsset config = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UBGWDataAsset>(configAssetPath, ELoadResourceType.SyncLoadAndCache);
        if (config == null)
        {
            Logging.LogError("Failed to load MagicallyChangeConfig from path: {Path}", configAssetPath);
            return;
        }
        Logging.LogDebug("Received trigger magically change for character {Nickname} with config {ConfigAssetPath}, skillID {SkillID}, recoverSkillID {RecoverSkillID}, curVigorSkillID {CurVigorSkillID}, castReason {CastReason}", pawn.GetName(), configAssetPath, skillID, recoverSkillID, curVigorSkillID, castReason);
        BUC_MagicallyChangeData magicallyChangeData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_MagicallyChangeData, BUC_MagicallyChangeData>(pawn);
        magicallyChangeData.CastReason = castReason;
        magicallyChangeData.CurVigorSkillID = curVigorSkillID;
        magicallyChangeData.bIsPendingCast = true;
        magicallyChangeData.bIsPendingReset = false;
        magicallyChangeData.PendingConfig = config;
        magicallyChangeData.MagicallyChangeSkillID = skillID;
        magicallyChangeData.RecoverSkillID = recoverSkillID;
    }

    public static void ResetMagicallyChange(BGUCharacterCS pawn, EResetReason_MagicallyChange reason)
    {
        Logging.LogDebug("Received reset magically change for character {Nickname} with reason {Reason}", pawn.GetName(), reason);
        BUC_MagicallyChangeData magicallyChangeData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_MagicallyChangeData, BUC_MagicallyChangeData>(pawn);
        magicallyChangeData.bIsPendingReset = true;
        magicallyChangeData.bIsPendingCast = false;
        magicallyChangeData.ResetReason = reason;
    }
}
