using System.Collections.Concurrent;
using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    public static class SummonPatch
    {
        private static readonly ConcurrentDictionary<NetworkId, ConcurrentQueue<FServantReq>> _summonsQueues = new();

        public static void QueueServant(NetworkId summonerId, FServantReq summonReq)
        {
            Logging.LogDebug("Enqueueing summon for character {Id}, type: {Action}", summonerId, summonReq.TamerTemplate.GetPathName());

            _summonsQueues.AddOrUpdate(summonerId, _ => new ConcurrentQueue<FServantReq>([summonReq]), (_, queue) =>
            {
                queue.Enqueue(summonReq);
                return queue;
            });
        }

        public static void ExecuteSummon(NetworkId summonerId, NetworkId summonId, string guid, string tamerClassName, int teamId)
        {
            Logging.LogDebug("Executing summon for character {Id}, type: {Action}", summonerId, tamerClassName);

            if (!_summonsQueues.TryGetValue(summonerId, out var queue))
                return;

            if (queue.TryDequeue(out var item))
            {
                if (tamerClassName != item.TamerTemplate.PathName)
                {
                    Logging.LogError("Requested and enqueued tamer servants have different classes");
                    return;
                }

                item.ServantTamerGuid = guid;
                SpawnServant(summonId, guid, teamId, item.TamerTemplate, item.BornTransform, item, item.SafeClampToLand);
            }
        }

        public static string? SpawnServant(NetworkId summonId, string guid, int teamId, TSubclassOf<BUTamerActor> TamerClass, in FTransform InTransform, FServantReq InServantReq, bool SafeClampToLand = false)
        {
            Logging.LogDebug("Spawning servant: {TamerName}, with Guid {Guid}", TamerClass.Value.GetPathName(), guid);

            var world = GameUtils.GetWorld();
            if (world == null || TamerClass.Value == null)
            {
                return null;
            }

            if (BGWGameInstanceCS.TickingGameInstNetMode(world) == EGameInstNetMode.Client)
            {
                return null;
            }

            BUTamerActor? bUTamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, TamerClass.Value, InTransform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null) as BUTamerActor;
            if (bUTamerActor == null)
            {
                return null;
            }

            if (SafeClampToLand)
            {
                FVector fVector = bUTamerActor.BGUGetActorLocation();
                float scaledCapsuleHalfHeight = bUTamerActor.CapsuleComponent.GetScaledCapsuleHalfHeight();
                float scaledCapsuleRadius = bUTamerActor.CapsuleComponent.GetScaledCapsuleRadius();
                FVector start = fVector + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
                FVector end = fVector - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
                List<AActor> list = [bUTamerActor];
                if (USystemLibrary.CapsuleTraceSingleByProfile(world, start, end, scaledCapsuleRadius, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, list, EDrawDebugTrace.None, out var OutHit, bIgnoreSelf: true, FLinearColor.Red, FLinearColor.Blue, 3f))
                {
                    FVector newLocation = BGUFunctionLibraryCS.BGUGetVectorFromNetQuantizeVector(in OutHit.ImpactPoint) + FVector.UpVector * scaledCapsuleHalfHeight;
                    bUTamerActor.BGUSetActorLocation(newLocation, bSweep: false, bTeleport: false);
                }
            }

            // Update final guid
            bUTamerActor.SpawnedTamerGuid = guid;
            bUTamerActor.GetFinalGuid(true);

            Logging.LogDebug("Spawned servant: {TamerName}, with Guid {Guid}", bUTamerActor.GetName(), guid);

            var entity = default(Entity); // TODO: The SendSummon event is never sent
            // var entity = SpawningUtils.AddRemoteMonsterToEcs(summonId, guid, bUTamerActor, teamId, TamerClass.Value.PathName);

            ref var transComp = ref entity.GetComponent<TranslationComponent>();
            transComp.Position = InServantReq.BornTransform.GetLocation().ToVector3();
            transComp.Rotation = InServantReq.BornTransform.Rotator().ToVector3();

            bUTamerActor.MarkAsServant();
            InServantReq.ServantTamerGuid = bUTamerActor.GetFinalGuid();
            BPS_EventCollectionCS.GetLocal(world).Evt_SendServantReq.Invoke(InServantReq);

            UBGUFunctionLibrary.BGUFinishSpawningActor(bUTamerActor, InTransform);
            return guid;
        }
    }

    //[HarmonyPatch(typeof(FSummonProcessor_Spawn), "RunProcessor")]
    //[HarmonyPatchCategory(Constants.GlobalPatches)]
    //public static class PatchSpawnRunProcessor
    //{
    //    public static void Prefix(FSummonInstance InSummonInstance)
    //    {
    //        if (!WukongMP.Instance.ShouldRunConnectedPatches())
    //            return;

    //        var client = WukongMpMod.Client;
    //        if (!client.IsMasterClient)
    //        {
    //            for (int i = 0; i < InSummonInstance.ServantReqList.Count; i++)
    //            {
    //                FServantReq fServantReq = InSummonInstance.ServantReqList[i];
    //                var summonerState = client.GetCharacterByActor(fServantReq.Summoner);
    //                if (summonerState != null)
    //                {
    //                    SummonPatch.QueueServant(summonerState.PeerId, fServantReq);
    //                }
    //            }
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(BGU_UnrealWorldUtil), nameof(BGU_UnrealWorldUtil.RequestSpawnServant))]
    //[HarmonyPatchCategory(Constants.GlobalPatches)]
    //public static class PatchRequestSpawnServant
    //{
    //    public static bool Prefix(ref string? __result, UWorld World, TSubclassOf<BUTamerActor> TamerClass, in FTransform InTransform, FServantReq InServantReq, bool SafeClampToLand = false)
    //    {
    //        if (!WukongMP.Instance.ShouldRunConnectedPatches())
    //            return true;

    //        var client = WukongMpMod.Client;
    //        if (!client.IsMasterClient)
    //        {
    //            __result = null;
    //            return false;
    //        }

    //        var id = -(client.SyncedMonsters.Count + client.RoomState.MaxPlayers);
    //        var ownerState = client.GetCharacterByActor(InServantReq.Summoner);
    //        if (ownerState == null)
    //        {
    //            Logging.LogDebug("Not synced chanracter {CharacterName} trying to summon tamer", InServantReq.Summoner.GetName());
    //            __result = null;
    //            return false;
    //        }

    //        var guid = Guid.NewGuid().ToString();
    //        __result = SummonPatch.SpawnServant(id, guid, ownerState.TeamId, TamerClass, InTransform, InServantReq, SafeClampToLand);
    //        Logging.LogDebug("Sending spawn summon for tamer {TamerPath}", TamerClass.Value.PathName);
    //        client.SpawnSummon(ownerState.PeerId, id, guid, TamerClass.Value.PathName, ownerState.TeamId);
    //        return false;
    //    }
    //}
}