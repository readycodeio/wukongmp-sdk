using System.Collections.Generic;
using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
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

                __result = null;
                if (DI.Instance.AreaState.IsMasterClient)
                {
                    var tamerActor = SpawningUtils.BeginDeferredSummonSpawn(World, TamerClass, InTransform, InServantReq.SummonID, SafeClampToLand);
                    if (tamerActor == null)
                    {
                        __result = null;
                        return false;
                    }
                    tamerActor.MarkAsServant();
                    InServantReq.ServantTamerGuid = tamerActor.GetFinalGuid();
                    BPS_EventCollectionCS.GetLocal(World).Evt_SendServantReq.Invoke(InServantReq);
                    UBGUFunctionLibrary.BGUFinishSpawningActor(tamerActor, InTransform);
                    __result = InServantReq.ServantTamerGuid;

                    // Add spawned monster to the ECS and send spawn request
                    SpawningUtils.CreateMonsterInEcs(__result, tamerActor, Constants.DefaultMonsterTeamId, tamerActor.PathName);
                    var summonerNetId = DI.Instance.PawnState.GetNetworkIdByActor(InServantReq.Summoner);
                    if (summonerNetId.HasValue)
                    {
                        DI.Instance.Rpc.SendSpawnSummon(InServantReq.FromGame());
                    }
                }
                return false;
            }
        }
    }
}