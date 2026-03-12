using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.State;

namespace WukongMp.Api.WukongUtils
{
    internal static class TamerUtils
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

        public static string UnifyUnitName(string unitName)
        {
            return unitName.ToLower().Replace("-", "").Replace("_", "");
        }

        public static void SpawnMonsterLocally(TamerEntity tamerEntity)
        {
            ref var tamerComp = ref tamerEntity.GetTamer();

            var bgsEvents = BGS_EventCollectionCS.Get(tamerEntity.Tamer);
            bgsEvents?.Evt_TamerBlockingSpawnImmediately.Invoke(tamerComp.Guid);
        }

        public static void DiscoverTamers(WukongPawnState pawnState)
        {
            Logging.LogDebug("Discovering tamers...");

            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                var tamerRef = actor.CurrentRef;
                var guid = BGU_DataUtil.GetActorGuid(actor);
                Logging.LogDebug("Monster: {Name}, alive: {Flag}, phase {Phase}, type {Type}, guid: {Guid}", actor.GetName(), actor.GetMonster() != null, tamerRef.Phase, tamerRef.TamerType, guid);
                var entity = pawnState.GetEntityByTamerGuid(guid);
                if (entity == null)
                {
                    SpawningUtils.CreateMonsterInEcs(pawnState, guid, actor, Constants.DefaultMonsterTeamId, actor.PathName);
                }
                else
                {
                    Logging.LogDebug("Monster already exists in ECS: {NetId}, guid {Guid}", entity.Value.GetMeta().NetId, entity.Value.GetTamer().Guid);
                }
            }
        }

        public static void MarkMonsterLocallySpawned(IMappedEventManager mappedEvent, TamerEntity tamerEntity)
        {
            ref var localTamerComp = ref tamerEntity.GetLocalTamer();

            if (!localTamerComp.IsLocallySpawned)
            {
                Logging.LogDebug("Sending UnitSpawn for tamer with guid: {Guid} (Entity {Entity})", BGU_DataUtil.GetActorGuid(tamerEntity.Tamer), tamerEntity.Entity.GetNetId());
                localTamerComp.IsLocallySpawned = true;

                var playerId = DI.Instance.PlayerState.LocalPlayerId ?? default;
                mappedEvent.InvokeInGameAndNotifyEcs(new UnitSpawnedEvent(tamerEntity.Entity, playerId), default(EmptyContext));
            }
        }

        public static void MarkMonsterLocallyDespawned(IMappedEventManager mappedEvent, TamerEntity tamerEntity)
        {
            ref var localTamerComp = ref tamerEntity.GetLocalTamer();

            if (localTamerComp.IsLocallySpawned)
            {
                Logging.LogDebug("Sending UnitDespawn for tamer with guid: {Guid} (Entity {Entity})", BGU_DataUtil.GetActorGuid(tamerEntity.Tamer), tamerEntity.Entity.GetNetId());
                localTamerComp.IsLocallySpawned = false;

                var playerId = DI.Instance.PlayerState.LocalPlayerId ?? default;
                mappedEvent.InvokeInGameAndNotifyEcs(new UnitDespawnedEvent(tamerEntity.Entity, playerId), default(EmptyContext));
            }
        }

        public static void AddSpawnedUnitRefCount(TamerEntity tamerEntity, PlayerId playerId)
        {
            ref var tamerComp = ref tamerEntity.GetTamer();
            var metaComp = tamerEntity.GetMeta();
            Logging.LogDebug("Adding spawned unit counter for tamer with guid: {Guid} (NetId {NetId}) for player {Player}", tamerComp.Guid, metaComp.NetId, playerId);
            tamerComp.HoldingPlayers = tamerComp.HoldingPlayers.Add(playerId);
        }

        public static void SubtractSpawnedUnitRefCount(TamerEntity tamerEntity, PlayerId playerId)
        {
            var metaComp = tamerEntity.GetMeta();
            ref var tamerComp = ref tamerEntity.GetTamer();
            Logging.LogDebug("Subtracting spawned unit counter for tamer with guid: {Guid} (NetId {NetId}) for player {Player}", tamerComp.Guid, metaComp.NetId, playerId);
            SubtractSpawnedUnitRefCount(ref tamerComp, playerId);
        }

        public static void SubtractSpawnedUnitRefCount(ref TamerComponent tamerComp, PlayerId playerId)
        {
            tamerComp.HoldingPlayers = tamerComp.HoldingPlayers.Remove(playerId);
        }

        public static void ClearSpawnedUnitRefCount(TamerEntity tamerEntity)
        {
            ref var tamerComp = ref tamerEntity.GetTamer();
            var metaComp = tamerEntity.GetMeta();
            Logging.LogDebug("Clearing spawned unit counter for tamer with guid: {Guid} (NetId {NetId})", tamerComp.Guid, metaComp.NetId);
            tamerComp.HoldingPlayers = tamerComp.HoldingPlayers.Clear();
            ref var localTamer = ref tamerEntity.GetLocalTamer();
            localTamer.IsLocallySpawned = false;
        }

        public static void TriggerSkillInteract(Entity entity, int skillId)
        {
            Logging.LogDebug("TriggerInteract for entity: {Entity}", entity.GetNetId());
            var tamerEntity = new TamerEntity(entity);
            BUS_EventCollectionCS.Get(tamerEntity.Pawn).Evt_UnitCastSkillTryCMultiCast.Invoke(new FCastSkillInfo(skillId, ECastSkillSourceType.Interact));
        }

        public static void DestroyTamer(string guid, BUTamerActor? tamerActor, AActor? markerActor)
        {
            tamerActor?.CurrentRef?.DestroyTamer();
            if (!markerActor.IsNullOrDestroyed())
            {
                Logging.LogDebug("Destroying marker for tamer with guid {Guid}", guid);
                BGU_UnrealWorldUtil.DestroyActor(markerActor);
            }
        }

        public static void TriggerWakeUp(BGUCharacterCS character)
        {
            var events = BUS_EventCollectionCS.Get(character);
            events?.Evt_OnWakeUp.Invoke();
        }

        public static void TeleportTamer(TamerEntity tamerEntity, FVector location, FRotator rotation)
        {
            var pawn = tamerEntity.Pawn;
            if (pawn == null)
            {
                Logging.LogError("Failed to teleport tamer: Pawn is null");
                return;
            }

            BUS_EventCollectionCS.Get(pawn)?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportBegin, -1f);
            var correctedLocation = SpawningUtils.GetCorrectedSpawnLocation(pawn, location);
            pawn.SetActorTransform(new FTransform(rotation, correctedLocation), false, out _, true);
        }
    }
}