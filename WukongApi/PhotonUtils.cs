using System.Collections.Generic;
using b1;
using BtlShare;
using UnrealEngine.Engine;
using WukongApi.State;

namespace WukongApi
{
    public static class PhotonUtils
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
                    SyncMonsterAndNotify(WukongMP.Instance.Photon, actor); // Neutral monsters
                }
            }
        }

        /// <summary>
        /// Register a spawned monster in Photon and notify other clients.
        /// If successful, the monster will be prepared for syncing.
        /// </summary>
        public static void SyncMonsterAndNotify(WukongClient photon, BUTamerActor tamer)
        {
            var monster = tamer.GetMonster();
            var guid = BGU_DataUtil.GetActorGuid(monster);

            // register in Photon if not present
            var monsterState = photon.GetByTamerActor(tamer);

            if (monsterState == null)
            {
                //monsterState = new MonsterState(guid, tamer);
                //photon.SyncedMonsters.Add(guid, monsterState);
                Logging.LogWarning("Local monster not registered in Photon: {MonsterGuid}", guid);
                return;
            }
            // sanity check guid
            else if (monsterState.Guid != guid)
            {
                Logging.LogError("Guid mismatch: {Guid1} != {Guid2}", monsterState.Guid, guid);
                return;
            }

            if (!monsterState.IsSynced)
            {
                // notify other clients
                photon.SendMonsterWakeUp(guid);
                PrepareMonsterForSync(photon, monsterState);
            }
        }

        /// <summary>
        /// Prepare a monster for syncing.
        /// </summary>
        public static void PrepareMonsterForSync(WukongClient photon, MonsterState monsterState)
        {
            if (monsterState.IsSynced)
            {
                Logging.LogError("Attempting to prepare monster that is already synced.");
                return;
            }

            var monster = monsterState.Pawn?.GetMonster();

            // sanity check
            if (monster == null)
            {
                Logging.LogError("Monster is null");
                return;
            }

            if (photon.IsMasterClient)
            {
                // subscribe to events on master
                var events = BUS_EventCollectionCS.Get(monsterState.Pawn);
                events.Evt_PlayMontageCallback += (reason, montage, state) =>
                {
                    var montagePath = montage.GetPathName();
                    Logging.LogDebug("Monster montage callback: {Guid} {Reason} {Montage} {State}", monsterState.Guid, reason, montagePath, state);
                    photon.SendMonsterMontageCallback(monsterState.Guid, reason, montagePath, state);
                };

                // also, set HP
                var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(monster);
                monsterState.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
            }
            else
            {
                // disable AI on clients
                var events = BUS_EventCollectionCS.Get(monster);

                if (events == null)
                {
                    Logging.LogError("events are null");
                    return;
                }

                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                events.Evt_AIPauseFsm.Invoke(true);
                events.Evt_EnableCanUpdateHatred.Invoke(P1: false);
                events.Evt_EnableCanSetBT.Invoke(P1: false);
            }

            Logging.LogDebug("Tamer actor disabled.");
            RegisterNewPlayerTeam(monster, monsterState.TeamId);

            // at this point the monster exists, so we set IsSpawned
            monsterState.IsSynced = true;
        }
    }
}