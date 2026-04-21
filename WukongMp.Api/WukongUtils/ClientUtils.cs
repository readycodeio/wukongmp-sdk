using System;
using System.Collections.Generic;
using System.Reflection;
using b1;

namespace WukongMp.Api.WukongUtils;

internal static class ClientUtils
{
    private static Action<BGUCharacterCS, int>? _setter;

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

}