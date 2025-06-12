using b1;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using System.Collections.Generic;
using ReadyM.Relay.Common;
using UnrealEngine.Engine;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;

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
                if (actor != null && actor.GetMonster() != null)
                {
                    BGU_UnrealWorldUtil.DestroyActor(actor.GetMonster());
                }

                BGU_UnrealWorldUtil.DestroyActor(actor);
            }
        }

        public static string UnifyUnitName(string unitName)
        {
            return unitName.ToLower().Replace("-", "").Replace("_", "");
        }

        public static void WakeUpMonster(Entity tamerEntity)
        {
            var localTamerComp = tamerEntity.GetComponent<LocalTamerComponent>();
            Logging.LogDebug("WakeUpMonster for tamer: {Guid}", BGU_DataUtil.GetActorGuid(localTamerComp.Tamer));

            var monster = localTamerComp.Tamer?.GetMonster();
            if (monster == null)
            {
                ref var tamerComp = ref tamerEntity.GetComponent<TamerComponent>();
                var bgsEvents = BGS_EventCollectionCS.Get(localTamerComp.Tamer);
                Logging.LogDebug("Monster {Guid} forced to spawn", tamerComp.Guid);
                bgsEvents?.Evt_TamerBlockingSpawnImmediately.Invoke(tamerComp.Guid);
            }
        }

        public static void DiscoverTamers()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            if (WukongMpModBase.Client.IsMasterClient)
            {
                foreach (var actor in allActorsOfClass)
                {
                    var tamerRef = actor.CurrentRef;
                    var guid = BGU_DataUtil.GetActorGuid(actor);
                    Logging.LogDebug("Monster: {Name}, alive: {Flag}, phase {Phase}, type {Type}, guid: {Guid}", actor.GetName(), actor.GetMonster() != null, tamerRef.Phase, tamerRef.TamerType, guid);
                    var entity = WukongMpMod.Instance.GetMonsterByGuid(guid);
                    if (entity == null)
                    {
                        SpawningUtils.CreateMonsterInEcs(guid, actor, 2, actor.PathName);
                    }
                    else
                    {
                        Logging.LogDebug("Monster already exists in ECS: {Entity}", entity.ToString());
                    }
                }
            }
        }

        public static void ClearEcsMonsters()
        {
            WukongMpMod.Instance.World.Query<LocalTamerComponent>().ForEachEntity((ref _, entity) => { WukongMpMod.Instance.CommandBuffer.DeleteEntity(entity.Id); });
        }

        public static void DestroyMonster(Entity entity)
        {
            var tamerComp = entity.GetComponent<LocalTamerComponent>();

            if (tamerComp.Tamer == null)
            {
                return;
            }

            var monsterPawn = tamerComp.Tamer.GetMonster();
            if (monsterPawn != null)
            {
                var events = BUS_EventCollectionCS.Get(monsterPawn);
                events.Evt_UnitDead.Invoke(null, EDeadReason.OnlyDestroyUnit);
                BGU_UnrealWorldUtil.DestroyActor(tamerComp.Pawn);
            }

            BGU_UnrealWorldUtil.DestroyActor(tamerComp.Tamer);

            CleanupMonster(entity);
        }

        public static void CleanupMonster(Entity entity)
        {
            var markerComp = entity.GetComponent<MarkerComponent>();

            if (markerComp.MarkerActor != null)
            {
                BGU_UnrealWorldUtil.DestroyActor(markerComp.MarkerActor);
            }

            Logging.LogDebug("Deleting entity from ECS: {Entity} (UnitDead)", entity.ToString());
            WukongMpMod.Instance.CommandBuffer.DeleteEntity(entity.Id);
        }

        public static void AddSpawnedUnit(PlayerId playerId, Entity entity)
        {
            Logging.LogDebug("Adding spawned unit counter for entity: {Entity}", entity.ToString());
            ref var localTamerComp = ref entity.GetComponent<LocalTamerComponent>();
            localTamerComp.HoldingPlayers.Add(playerId);
            ref var tamerComp = ref entity.GetComponent<TamerComponent>();
            tamerComp.ShouldBeSpawned = true;
        }

        public static void SubtractSpawnedUnit(PlayerId playerId, Entity entity)
        {
            Logging.LogDebug("Subtracting spawned unit counter for entity: {Entity}", entity.ToString());
            ref var localTamerComp = ref entity.GetComponent<LocalTamerComponent>();
            ref var tamerComp = ref entity.GetComponent<TamerComponent>();
            SubtractSpawnedUnit(playerId, ref localTamerComp, ref tamerComp);
        }

        public static void SubtractSpawnedUnit(PlayerId playerId, ref LocalTamerComponent localTamerComp, ref TamerComponent tamerComp)
        {
            localTamerComp.HoldingPlayers.Remove(playerId);
            if (localTamerComp.HoldingPlayers.Count == 0)
            {
                tamerComp.ShouldBeSpawned = false;
            }
        }

        public static void ClearSpawnedUnit(Entity entity)
        {
            Logging.LogDebug("Clearing spawned unit counter for entity: {Entity}", entity.ToString());
            ref var localTamerComp = ref entity.GetComponent<LocalTamerComponent>();
            localTamerComp.HoldingPlayers.Clear();
            ref var tamerComp = ref entity.GetComponent<TamerComponent>();
            tamerComp.ShouldBeSpawned = false;
        }
    }
}