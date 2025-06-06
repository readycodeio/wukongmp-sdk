using b1;
using BtlShare;
using Friflo.Engine.ECS;
using System.Collections.Generic;
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

        public static void WakeUpMonster(BUTamerActor tamerActor)
        {
            Logging.LogWarning("WakeUpMonster for tamer: {Guid}", BGU_DataUtil.GetActorGuid(tamerActor));
            var monster = tamerActor.GetMonster();
            if (monster == null)
            {
                var bgsEvents = BGS_EventCollectionCS.Get(tamerActor);
                bgsEvents?.Evt_TamerBlockingSpawnImmediately.Invoke(BGU_DataUtil.GetActorGuid(tamerActor));
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
                    Logging.LogDebug("Monster: {Name}, alive: {Flag}, phase {Phase}, type {Type}, guid: {Guid}", actor.GetName(), actor.GetMonster() != null, tamerRef.Phase, tamerRef.TamerType, BGU_DataUtil.GetActorGuid(actor));
                    if (tamerRef.Phase != ETamerPhase.Dead)
                    {
                        SpawningUtils.CreateMonsterInEcs(BGU_DataUtil.GetActorGuid(actor), actor, 2, actor.PathName);
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
    }
}