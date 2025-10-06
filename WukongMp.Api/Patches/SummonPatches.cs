using b1;
using Friflo.Engine.ECS;
using HarmonyLib;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System.Numerics;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGU_UnrealWorldUtil), nameof(BGU_UnrealWorldUtil.RequestSpawnServant))]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchRequestSpawnServant
{
    public static bool Prefix(ref string? __result, UWorld World, TSubclassOf<BUTamerActor> TamerClass, in FTransform InTransform, FServantReq InServantReq, bool SafeClampToLand = false)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (InServantReq.ServantType == EServantType.NeutralAnimSpawn || InServantReq.ServantType == EServantType.PhantomRush)
            return true;

        __result = null;
        if (CanSummon(InServantReq.Summoner, InTransform.GetLocation()))
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
            SpawningUtils.CreateMonsterInEcs(__result, tamerActor, Constants.DefaultMonsterTeamId, tamerActor.PathName);
            Logging.LogDebug("Sending SpawnSummon for summoner {Summoner} with guid {Guid} for tamer path {Path}", InServantReq.Summoner?.GetName() ?? "Null", InServantReq.ServantTamerGuid, InServantReq.TamerTemplate.GetName());
            DI.Instance.Rpc.SendSpawnSummon(InServantReq.FromGame());
        }
        return false;
    }

    private static bool CanSummon(AActor summoner, FVector summonLocation)
    {
        var localCharacter = DI.Instance.PlayerState.LocalMainCharacter;
        if (localCharacter == null) 
        { 
            return false;
        }
        var summonerEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(summoner);
        if (summonerEntity.HasValue && summoner == localCharacter.Value.GetLocalState().Pawn)
        {
            return true; // Local player summons.
        }
        else if (summonerEntity.HasValue)
        {
            return false; // Other player summons.
        }
        else // Summoner is not a player e.g. spawn point
        {
            if (DI.Instance.PlayerState.LocalPlayerId == null)
                return false;

            var localPlayerId = DI.Instance.PlayerState.LocalPlayerId.Value;
            var localPosition = localCharacter.Value.GetState().Location;
            var squaredDistanceToSummon = FVector.DistSquared(localPosition.ToFVector(), summonLocation);
            var squaredSpawnOwnershipDistance = Constants.SpawnOwnershipDistance * Constants.SpawnOwnershipDistance;
            if (squaredDistanceToSummon > squaredSpawnOwnershipDistance)
            {
                return DI.Instance.AreaState.IsMasterClient; // Distant summon -> master as owner
            }

            // Check if master or another player with lower id is nearby
            bool canSpawn = true;
            DI.Instance.World.Query<MainCharacterComponent>().ForEachEntity((
            ref MainCharacterComponent playerComp, Entity entity) =>
            {
                if (entity == localCharacter.Value.Entity)
                    return;

                var squaredDistance = Vector3.DistanceSquared(localPosition, playerComp.Location);
                if (squaredDistance < squaredSpawnOwnershipDistance && (DI.Instance.AreaState.MasterClientId == playerComp.PlayerId || playerComp.PlayerId.RawValue < localPlayerId.RawValue))
                {
                    canSpawn = false;
                }
            });
            return canSpawn;
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), "OnUnload")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTamerUnload
{
    public static void Postfix(FTamerRef __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (__instance.TamerType != ETamerType.Summoned)
            return;

        var tamerEntity = DI.Instance.PawnState.GetByEntityByTamer(__instance.InstancePtr.Value);
        if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
        {
            Logging.LogDebug("Deleting tamer entity from ECS: {Entity} (OnUnload)", tamerEntity.Value.ToString());
            DI.Instance.EcsLoop.CommandBuffer.DeleteEntity(tamerEntity.Value.Entity.Id);
        }
    }
}
