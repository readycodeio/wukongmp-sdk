using b1;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace WukongMp.Api.WukongUtils;

// TODO: More like: TeamUtils
public static class ClientUtils
{
    private static Action<BGUCharacterCS, int>? _setter;

    public static void RegisterTeamHostility(int team1, int team2)
    {
        var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

        EnsureTeamRelationExists(teamRelationData, team1);
        EnsureTeamRelationExists(teamRelationData, team2);

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

        EnsureTeamRelationExists(teamRelationData, team1);
        EnsureTeamRelationExists(teamRelationData, team2);

        var team1RelationInfo = teamRelationData.TeamHostileInfos[team1];
        var team2RelationInfo = teamRelationData.TeamHostileInfos[team2];

        team1RelationInfo.HostileTeamIDs.Remove(team2);
        team2RelationInfo.HostileTeamIDs.Remove(team1);
    }

    public static void RegisterAndSetPlayerTeam(BGUCharacterCS actor, int newTeamId)
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

        if (_setter == null)
        {
            var setterMethod = typeof(BGUCharacterCS).GetProperty("TeamIDInCS", BindingFlags.Instance | BindingFlags.NonPublic)!.GetSetMethod(true);
            _setter = (Action<BGUCharacterCS, int>)Delegate.CreateDelegate(typeof(Action<BGUCharacterCS, int>), setterMethod!);
        }
        Logging.LogInformation("Setting team id {Team} for actor {Actor}", newTeamId, actor.GetName());
        _setter.Invoke(actor, newTeamId);
        actor.SetTeamIDInCS(newTeamId);
    }

    private static void EnsureTeamRelationExists(BGC_TeamRelationData teamRelationData, int teamId)
    {
        if (!teamRelationData.TeamHostileInfos.ContainsKey(teamId))
        {
            teamRelationData.TeamHostileInfos.Add(teamId, new TeamRelationInfo());
        }
    }
}