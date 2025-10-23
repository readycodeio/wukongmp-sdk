using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems.PvP;

public sealed class PlayerListSystem(WukongPlayerState playerState, WukongEventBus eventBus) : QuerySystem<MainCharacterComponent, PvPComponent>
{
    private readonly Stopwatch _timer = Stopwatch.StartNew();

    protected override void OnUpdate()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (_timer.Elapsed < TimeSpan.FromSeconds(1))
            return;

        _timer.Restart();

        List<string> redTeamList = [];
        List<string> blueTeamList = [];
        List<string> spectatorsList = [];

        Query.ForEachEntity((ref MainCharacterComponent mainCharacterComponent, ref PvPComponent pvp, Entity _) =>
        {
            var player = playerState.GetPlayerById(mainCharacterComponent.PlayerId);
            if (player.HasValue)
            {
                var team = player.Value.GetState().TeamId;
                if (pvp.IsSpectator)
                {
                    spectatorsList.Add(mainCharacterComponent.CharacterNickName);
                    return;
                }
                else if (team == Constants.AvailableTeamIds[0])
                {
                    redTeamList.Add(mainCharacterComponent.CharacterNickName);
                    return;
                }
                else if (team == Constants.AvailableTeamIds[1])
                {
                    blueTeamList.Add(mainCharacterComponent.CharacterNickName);
                    return;
                }
            }
        });

        LobbyStatusWidget.Instance.SetTeams(redTeamList, blueTeamList, spectatorsList);
        LobbyStatusWidget.Instance.SetConnectedCount(Query.Count);
    }
}