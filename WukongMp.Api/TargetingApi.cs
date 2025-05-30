using b1;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api;

public static class TargetingApi
{
    public static void SetTarget(BGUCharacterCS pawn, BGUCharacterCS target)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            Logging.LogDebug("Updating target for pawn {Pawn} to pawn {Pawn}", pawn.PathName, target.PathName);
            var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
            targetInfoData.SetTargetInfo(new UnitLockTargetInfo(target, ETargetSourceType.SkillBase_NormalUse));
        }, nameof(SetTarget));
    }

    public static void ClearTarget(BGUCharacterCS pawn)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            Logging.LogDebug("Updating target for pawn {Pawn} to null", pawn.PathName);
            var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
            targetInfoData.SetTargetInfo(new UnitLockTargetInfo());
        }, nameof(ClearTarget));
    }
}