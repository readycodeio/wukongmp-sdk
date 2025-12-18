using b1;
using UnrealEngine.Engine;

namespace WukongMp.Api.WukongUtils;

public static class TargetingUtils
{
    public static AActor? GetTarget(BGUCharacterCS? pawn)
    {
        if (pawn == null)
            return null;
        
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        return targetInfoData.GetTargetInfo()?.LockTargetActor;
    }
    
    public static AActor? GetAoTarget(BGUCharacterCS? pawn)
    {
        if (pawn == null)
            return null;
        
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        return targetInfoData.GetAOTarget()?.LockTargetActor;
    }
    
    public static void SetTarget(BGUCharacterCS pawn, BGUCharacterCS target)
    {
        Logging.LogDebug("Updating target for pawn {Pawn} to pawn {Target}", BGU_DataUtil.GetActorGuid(pawn), BGU_DataUtil.GetActorGuid(target));
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.SetTargetInfo(new UnitLockTargetInfo(target, ETargetSourceType.SkillBase_NormalUse));
    } 
    
    public static void SetAOTarget(BGUCharacterCS pawn, BGUCharacterCS target, ETargetSourceType sourceType, bool bPlayer, float degreeLimit)
    {
        Logging.LogDebug("Updating AO target for pawn {Pawn} to pawn {Target}", BGU_DataUtil.GetActorGuid(pawn), BGU_DataUtil.GetActorGuid(target));
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.SetAOTarget(target, sourceType, bPlayer, degreeLimit);
    }

    public static void ClearTarget(BGUCharacterCS pawn)
    {
        Logging.LogDebug("Updating target for pawn {Pawn} to null", BGU_DataUtil.GetActorGuid(pawn));
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.SetTargetInfo(new UnitLockTargetInfo());
    }
    
    public static void ClearAOTarget(BGUCharacterCS pawn)
    {
        Logging.LogDebug("Updating AO target for pawn {Pawn} to null", BGU_DataUtil.GetActorGuid(pawn));
        var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
        targetInfoData.ClearAOTarget();
    }
}