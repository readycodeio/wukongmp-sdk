using System.Collections.Generic;
using b1;
using BtlShare;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong;
using UnrealEngine.Engine;
using WukongApi.ECS;
using WukongApi.State;

namespace WukongApi
{
    public static class ClientUtils
    {
        public static void RegisterTeamHostility(int team1, int team2)
        {
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            var team1RelationInfo = teamRelationData.TeamHostileInfos[team1];
            var team2RelationInfo = teamRelationData.TeamHostileInfos[team2];

            if (!team1RelationInfo.HostileTeamIDs.Contains(team2))
            {
                team1RelationInfo.HostileTeamIDs.Add(team2);
            }

            if (!team2RelationInfo.HostileTeamIDs.Contains(team1))
            {
                team2RelationInfo.HostileTeamIDs.Add(team1);
            }

            // TODO: set damage reduction ratio
        }

        public static void UnregisterTeamHostility(int team1, int team2)
        {
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            var team1RelationInfo = teamRelationData.TeamHostileInfos[team1];
            var team2RelationInfo = teamRelationData.TeamHostileInfos[team2];

            team1RelationInfo.HostileTeamIDs.Remove(team2);
            team2RelationInfo.HostileTeamIDs.Remove(team1);

            // TODO: unset damage reduction ratio
        }

        public static void RegisterNewPlayerTeam(BGUCharacterCS actor, int newTeamId)
        {
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            if (!teamRelationData.TeamHostileInfos.ContainsKey(newTeamId))
            {
                var oldTeamId = actor.GetTeamIDInCS();
                var oldRelationInfo = teamRelationData.TeamHostileInfos[oldTeamId];

                var newRelationInfo = new TeamRelationInfo
                {
                    HostileTeamIDs = [..oldRelationInfo.HostileTeamIDs],
                    TeamDamageReductionRatios = new Dictionary<int, int>(oldRelationInfo.TeamDamageReductionRatios)
                };
                teamRelationData.TeamHostileInfos.Add(newTeamId, newRelationInfo);
            }

            actor.SetTeamIDInCS(newTeamId);
        }

        public static void DiscoverMonsters()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                if (actor.GetMonster() != null)
                {
                    Logging.LogDebug("Discovered monster: {Guid}", BGU_DataUtil.GetActorGuid(actor.GetMonster()));
                    SyncMonsterAndNotify(WukongMP.Instance.Client, actor); // Neutral monsters
                }
            }
        }

        /// <summary>
        /// Register a spawned monster and notify other clients.
        /// If successful, the monster will be prepared for syncing.
        /// </summary>
        public static void SyncMonsterAndNotify(WukongClient client, BUTamerActor tamer)
        {
            var monster = tamer.GetMonster();
            var guid = BGU_DataUtil.GetActorGuid(monster);

            // register if not present
            var entity = client.GetByTamerActor(tamer);

            if (!entity.HasValue)
            {
                //monsterState = new MonsterState(guid, tamer);
                //client.SyncedMonsters.Add(guid, monsterState);
                Logging.LogWarning("Local monster not registered: {MonsterGuid}", guid);
                return;
            }

            // sanity check guid

            ref var tamerComp = ref client.GetEntityComponent<TamerComponent>(entity.Value);

            if (tamerComp.Guid != guid)
            {
                Logging.LogError("Guid mismatch: {Guid1} != {Guid2}", tamerComp.Guid, guid);
                return;
            }

            if (!tamerComp.IsSynced)
            {
                // notify other clients
                client.SendMonsterWakeUp(guid);
                PrepareMonsterForSync(client, entity.Value, ref tamerComp);
            }
        }

        /// <summary>
        /// Prepare a monster for syncing.
        /// </summary>
        private static void PrepareMonsterForSync(WukongClient client, EntityId entity, ref TamerComponent monsterState)
        {
            if (monsterState.IsSynced)
            {
                Logging.LogError("Attempting to prepare monster that is already synced.");
                return;
            }

            var monster = monsterState.Tamer?.GetMonster();

            // sanity check
            if (monster == null)
            {
                Logging.LogError("Monster is null");
                return;
            }

            // set monster hp
            var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(monster);
            
            ref var hpComp = ref client.GetEntityComponent<HpComponent>(entity);
            hpComp.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);

            var events = BUS_EventCollectionCS.Get(monsterState.Tamer);
            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            IBUC_ABPMotionMatchingData mmData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPMotionMatchingData>(monsterState.Pawn);
            if (mmData == null)
            {
                Logging.LogError("motion matching data is null");
                return;
            }

            events.Evt_ChangeMotionMatchingState.Invoke(mmData.DefaultMMState);

            if (!client.IsMasterClient)
            {
                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                Logging.LogDebug("Tamer actor disabled.");
            }

            var teamId = client.GetEntityComponent<TeamComponent>(entity).TeamId;
            RegisterNewPlayerTeam(monster, teamId);

            // at this point the monster exists, so we set IsSpawned
            monsterState.IsSynced = true;
            Logging.LogDebug("Monster {Guid} synced", monsterState.Guid);
        }
    }
}