using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Mapping.Policies.Event;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGU_UnrealWorldUtil), nameof(BGU_UnrealWorldUtil.RequestSpawnServant))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRequestSpawnServant
{
    public static bool Prefix(ref string? __result, UWorld World, TSubclassOf<BUTamerActor> TamerClass, in FTransform InTransform, FServantReq InServantReq, bool SafeClampToLand = false)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (InServantReq.ServantType is EServantType.NeutralAnimSpawn or EServantType.PhantomRush)
            return true;

        __result = null;
        if (DI.Instance.MappingPolicyDir.IsCharacterMapped(InServantReq.Summoner, out var summoner))
        {
            var ctx = new SpawnSummonContext(summoner, InTransform.GetLocation());
            if (DI.Instance.MappingPolicyDir.ForEvent<SpawnSummonEvent, SpawnSummonContext>().CanGameEventNotifyEcs(ctx))
            {
                // inlined original code
                var tamerActor = SpawningUtils.BeginDeferredSummonSpawn(World, TamerClass, InTransform, InServantReq.SummonID, SafeClampToLand);
                if (tamerActor == null)
                {
                    return false; // we overwrite the original method
                }

                tamerActor.MarkAsServant();
                InServantReq.ServantTamerGuid = tamerActor.GetFinalGuid();
                BPS_EventCollectionCS.GetLocal(World).Evt_SendServantReq.Invoke(InServantReq);
                UBGUFunctionLibrary.BGUFinishSpawningActor(tamerActor, InTransform);
                __result = InServantReq.ServantTamerGuid;

                // Add spawned monster to the ECS and send spawn request
                var summonTeam = Constants.DefaultMonsterTeamId;
                if (InServantReq.MasterActor is BGUCharacterCS master)
                    summonTeam = master.GetTeamIDInCS();
                SpawningUtils.CreateMonsterInEcs(DI.Instance.PawnState, __result, tamerActor, summonTeam, tamerActor.PathName);
                Logging.LogDebug("Sending SpawnSummon for summoner {Summoner} with guid {Guid} for tamer path {Path}", InServantReq.Summoner?.GetName() ?? "Null", InServantReq.ServantTamerGuid, InServantReq.TamerTemplate.GetName());

                var ev = InServantReq.FromGame(DI.Instance.PawnState);
                if (ev != null)
                {
                    DI.Instance.MappedEvent.NotifyEcsIfApplicable(ev.Value, ctx);
                }
            }
        }

        return false; // we overwrite the original method
    }
}

[HarmonyPatch(typeof(BGS_SummonManagerSystem), "RequestSummon")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchRequestSummon
{
    public static bool Prefix(FSummonReq InSummonReq)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (InSummonReq.SummonType is ESummonType.NeutralAnimSpawn or ESummonType.PhantomRush)
            return true;

        if (!DI.Instance.MappingPolicyDir.IsCharacterMapped(InSummonReq.Summoner, out var summoner))
            return true;

        var ctx = new SpawnSummonContext(summoner, InSummonReq.HitLocation);
        return DI.Instance.MappingPolicyDir.ForEvent<SpawnSummonEvent, SpawnSummonContext>().CanGameEventRunLocally(ctx, out _);
    }
}