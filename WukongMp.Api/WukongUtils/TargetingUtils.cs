using b1;

namespace WukongMp.Api.WukongUtils;

public static class TargetingUtils
{
    public static void SetTarget(BGUCharacterCS pawn, BGUCharacterCS target)
    {
        Logging.LogDebug("Updating target for pawn {Pawn} to pawn {Target}", pawn.PathName, target.PathName);
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.SetTargetInfo(new UnitLockTargetInfo(target, ETargetSourceType.SkillBase_NormalUse));
    }

    public static void ClearTarget(BGUCharacterCS pawn)
    {
        Logging.LogDebug("Updating target for pawn {Pawn} to null", pawn.PathName);
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.SetTargetInfo(new UnitLockTargetInfo());
    }
}