using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
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
        if (SpawningUtils.CanSummon(
                DI.Instance.PlayerState,
                DI.Instance.AreaState,
                DI.Instance.PawnState,
                DI.Instance.World,
                InServantReq.Summoner,
                InTransform.GetLocation()))
        {
            var tamerActor = SpawningUtils.BeginDeferredSummonSpawn(World, TamerClass, InTransform, InServantReq.SummonID, SafeClampToLand);
            if (tamerActor == null)
            {
                return false;
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
            DI.Instance.ClientRpc.SendSpawnSummon(InServantReq.FromGame(DI.Instance.PawnState));
        }

        return false;
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

        return SpawningUtils.CanSummon(
            DI.Instance.PlayerState,
            DI.Instance.AreaState,
            DI.Instance.PawnState,
            DI.Instance.World,
            InSummonReq.Summoner,
            InSummonReq.HitLocation);
    }
}