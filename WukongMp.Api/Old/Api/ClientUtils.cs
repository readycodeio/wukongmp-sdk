using System.Collections.Generic;
using b1;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old.Api
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
        }

        public static void UnregisterTeamHostility(int team1, int team2)
        {
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            var team1RelationInfo = teamRelationData.TeamHostileInfos[team1];
            var team2RelationInfo = teamRelationData.TeamHostileInfos[team2];

            team1RelationInfo.HostileTeamIDs.Remove(team2);
            team2RelationInfo.HostileTeamIDs.Remove(team1);
        }

        public static void RegisterNewPlayerTeam(BGUCharacterCS actor, int newTeamId)
        {
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            if (!teamRelationData.TeamHostileInfos.ContainsKey(newTeamId))
            {
                var oldTeamId = actor.GetTeamIDInCS();
                if (!teamRelationData.TeamHostileInfos.TryGetValue(oldTeamId, out var oldRelationInfo))
                {
                    oldRelationInfo = new TeamRelationInfo
                    {
                        HostileTeamIDs = [],
                        TeamDamageReductionRatios = new Dictionary<int, int>()
                    };
                }

                var newRelationInfo = new TeamRelationInfo
                {
                    HostileTeamIDs = [..oldRelationInfo.HostileTeamIDs],
                    TeamDamageReductionRatios = new Dictionary<int, int>(oldRelationInfo.TeamDamageReductionRatios)
                };
                teamRelationData.TeamHostileInfos.Add(newTeamId, newRelationInfo);
            }

            actor.SetTeamIDInCS(newTeamId);
        }
    }
}