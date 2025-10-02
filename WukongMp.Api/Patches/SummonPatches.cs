using System.Collections.Generic;
using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    public static class SummonPatch
    {
        [HarmonyPatch(typeof(BGU_UnrealWorldUtil), nameof(BGU_UnrealWorldUtil.RequestSpawnServant))]
        [HarmonyPatchCategory(Constants.CoopPatches)]
        public static class PatchRequestSpawnServant
        {
            public static bool Prefix(ref string? __result, UWorld World, TSubclassOf<BUTamerActor> TamerClass, in FTransform InTransform, FServantReq InServantReq, bool SafeClampToLand = false)
            {
                if (!DI.Instance.AreaState.InRoom)
                    return true;

                if (InServantReq.ServantType == EServantType.PhantomRush || InServantReq.ServantType == EServantType.NeutralAnimSpawn || InServantReq.ServantType == EServantType.Clone)
                    return true;

                if (DI.Instance.AreaState.IsMasterClient)
                {
                    // Original implementation
                    if (World == null || TamerClass.Value == null)
                    {
                        __result = null;
                        return false;
                    }
                    if (BGWGameInstanceCS.TickingGameInstNetMode(World) == EGameInstNetMode.Client)
                    {
                        __result = null;
                        return false;
                    }
                    BUTamerActor? bUTamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(World, TamerClass.Value, InTransform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null) as BUTamerActor;
                    if (bUTamerActor == null)
                    {
                        __result = null;
                        return false;
                    }
                    if (SafeClampToLand)
                    {
                        FVector fVector = BGUFuncLibActorTransformCS.BGUGetActorLocation(bUTamerActor);
                        float scaledCapsuleHalfHeight = bUTamerActor.CapsuleComponent.GetScaledCapsuleHalfHeight();
                        float scaledCapsuleRadius = bUTamerActor.CapsuleComponent.GetScaledCapsuleRadius();
                        FVector start = fVector + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
                        FVector end = fVector - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
                        List<AActor> list = [bUTamerActor];
                        if (USystemLibrary.CapsuleTraceSingleByProfile(World, start, end, scaledCapsuleRadius, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, list, EDrawDebugTrace.None, out var OutHit, bIgnoreSelf: true, FLinearColor.Red, FLinearColor.Blue, 3f))
                        {
                            FVector newLocation = BGUFunctionLibraryCS.BGUGetVectorFromNetQuantizeVector(in OutHit.ImpactPoint) + FVector.UpVector * scaledCapsuleHalfHeight;
                            BGUFuncLibActorTransformCS.BGUSetActorLocation(bUTamerActor, newLocation, bSweep: false, bTeleport: false);
                        }
                    }
                    bUTamerActor.MarkAsServant();
                    InServantReq.ServantTamerGuid = bUTamerActor.GetFinalGuid();
                    BPS_EventCollectionCS.GetLocal(World).Evt_SendServantReq.Invoke(InServantReq);
                    if (B1Global.GIsBossRushMode)
                    {
                        IBIC_BossRushBattleData gameInstanceReadonlyData = BGU_DataUtil.GetGameInstanceReadonlyData<IBIC_BossRushBattleData, BIC_BossRushBattleData>(World);
                        if (gameInstanceReadonlyData != null && gameInstanceReadonlyData.ServantPropertyOverrideList.TryGetValue(InServantReq.SummonID, out var value))
                        {
                            bUTamerActor.ApplyServantPropertyOverride(value);
                        }
                    }
                    UBGUFunctionLibrary.BGUFinishSpawningActor(bUTamerActor, InTransform);
                    __result = InServantReq.ServantTamerGuid;
                    // Add spawned monster to the ECS and send spawn request
                    SpawningUtils.CreateMonsterInEcs(__result, bUTamerActor, Constants.DefaultMonsterTeamId, bUTamerActor.PathName);
                    var summonerNetId = DI.Instance.PawnState.GetNetworkIdByActor(InServantReq.Summoner);
                    if (summonerNetId.HasValue)
                    {
                        DI.Instance.Rpc.SendSpawnSummon(new DTO.UnitSummonData(summonerNetId.Value, InServantReq));
                    }
                }
                return false;
            }
        }
    }
}