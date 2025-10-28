using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System.Collections.Generic;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils
{
    public static class TamerUtils
    {
        public static IEnumerable<BGUCharacterCS> GetMonsters()
        {
            var world = GameUtils.GetWorld();
            if (world == null)
                yield break;

            var actors = world.GetAllActorsOfClass<BUTamerActor>();
            foreach (var actor in actors)
            {
                Logging.LogDebug("Found actor: {ActorName}", actor.GetName());

                var monster = actor.GetMonster();
                if (monster != null)
                {
                    Logging.LogDebug("Actor is a monster");
                    yield return monster;
                }
            }
        }

        public static void DestroyAllTamers()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                actor.CurrentRef.DestroyTamer();
            }
        }

        public static string UnifyUnitName(string unitName)
        {
            return unitName.ToLower().Replace("-", "").Replace("_", "");
        }

        public static void SpawnMonsterLocally(TamerEntity tamerEntity)
        {
            ref var localTamerComp = ref tamerEntity.GetLocalTamer();
            ref var tamerComp = ref tamerEntity.GetTamer();

            var bgsEvents = BGS_EventCollectionCS.Get(localTamerComp.Tamer);
            bgsEvents?.Evt_TamerBlockingSpawnImmediately.Invoke(tamerComp.Guid);
        }

        public static void DiscoverTamers()
        {
            Logging.LogDebug("Discovering tamers...");

            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                var tamerRef = actor.CurrentRef;
                var guid = BGU_DataUtil.GetActorGuid(actor);
                Logging.LogDebug("Monster: {Name}, alive: {Flag}, phase {Phase}, type {Type}, guid: {Guid}", actor.GetName(), actor.GetMonster() != null, tamerRef.Phase, tamerRef.TamerType, guid);
                var entity = DI.Instance.PawnState.GetEntityByTamerGuid(guid);
                if (entity == null)
                {
                    SpawningUtils.CreateMonsterInEcs(guid, actor, Constants.DefaultMonsterTeamId, actor.PathName);
                }
                else
                {
                    Logging.LogDebug("Monster already exists in ECS: {Entity}", entity.ToString());
                }
            }
        }

        public static void MarkMonsterLocallySpawned(ref LocalTamerComponent localTamer, MetadataComponent metadata)
        {
            if (!localTamer.IsLocallySpawned)
            {
                localTamer.IsLocallySpawned = true;
                DI.Instance.Rpc.SendUnitSpawned(metadata.NetId);
            }
        }

        public static void MarkMonsterLocallyDespawned(ref LocalTamerComponent localTamer, MetadataComponent metadata)
        {
            if (localTamer.IsLocallySpawned)
            {
                localTamer.IsLocallySpawned = false;
                DI.Instance.Rpc.SendUnitDespawn(metadata.NetId);
            }
        }

        public static void AddSpawnedUnitRefCount(PlayerId playerId, TamerEntity tamerEntity)
        {
            ref var tamerComp = ref tamerEntity.GetTamer();
            Logging.LogDebug("Adding spawned unit counter for tamer with guid: {Guid} for player {Player}", tamerComp.Guid, playerId);
            tamerComp.ShouldBeSpawned = true;
            tamerComp.HoldingPlayers = tamerComp.HoldingPlayers.Add(playerId);
        }

        public static void SubtractSpawnedUnitRefCount(PlayerId playerId, TamerEntity tamerEntity)
        {
            Logging.LogDebug("Subtracting spawned unit counter for entity: {Entity} for player {Player}", tamerEntity.ToString(), playerId);
            ref var tamerComp = ref tamerEntity.GetTamer();
            SubtractSpawnedUnitRefCount(playerId, ref tamerComp);
        }

        public static void SubtractSpawnedUnitRefCount(PlayerId playerId, ref TamerComponent tamerComp)
        {
            tamerComp.HoldingPlayers = tamerComp.HoldingPlayers.Remove(playerId);
            if (tamerComp.HoldingPlayers.Count == 0)
            {
                tamerComp.ShouldBeSpawned = false;
            }
        }

        public static void ClearSpawnedUnitRefCount(TamerEntity tamerEntity)
        {
            Logging.LogDebug("Clearing spawned unit counter for entity: {Entity}", tamerEntity.ToString());
            ref var tamerComp = ref tamerEntity.GetTamer();
            tamerComp.HoldingPlayers = tamerComp.HoldingPlayers.Clear();
            tamerComp.ShouldBeSpawned = false;
            ref var localTamer = ref tamerEntity.GetLocalTamer();
            localTamer.IsLocallySpawned = false;
        }

        public static void TriggerSkillInteract(Entity entity, int skillId)
        {
            Logging.LogDebug("TriggerInteract for entity: {Entity}", entity.ToString());
            ref var localTamerComp = ref entity.GetComponent<LocalTamerComponent>();
            BUS_EventCollectionCS.Get(localTamerComp.Pawn).Evt_UnitCastSkillTryCMultiCast.Invoke(new FCastSkillInfo(skillId, ECastSkillSourceType.Interact));
        }

        public static void DestroyTamer(string guid, BUTamerActor? tamerActor, AActor? markerActor)
        {
            tamerActor?.CurrentRef.DestroyTamer();
            if (!markerActor.IsNullOrDestroyed())
            {
                Logging.LogDebug("Destroying marker for tamer with guid {Guid}", guid);
                BGU_UnrealWorldUtil.DestroyActor(markerActor);
            }
        }
    }
}