using b1;

namespace WukongMp.Api.WukongUtils;

public static class TargetingUtils
{
    public static void SetTarget(BGUCharacterCS pawn, BGUCharacterCS target)
    {
        Logging.LogDebug("Updating target for pawn {Pawn} to pawn {Target}", BGU_DataUtil.GetActorGuid(pawn), BGU_DataUtil.GetActorGuid(target));
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.SetTargetInfo(new UnitLockTargetInfo(target, ETargetSourceType.SkillBase_NormalUse));
    }

    public static void ClearTarget(BGUCharacterCS pawn)
    {
        Logging.LogDebug("Updating target for pawn {Pawn} to null", BGU_DataUtil.GetActorGuid(pawn));
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.SetTargetInfo(new UnitLockTargetInfo());
    }
}