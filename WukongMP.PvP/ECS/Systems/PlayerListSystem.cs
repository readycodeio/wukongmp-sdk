using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;
using WukongMp.PvP.UI;

namespace WukongMp.PvP.ECS.Systems;

internal sealed class PlayerListSystem(
    WukongPlayerState playerState,
    WukongAreaState areaState,
    PvpWidgetManager widgetManager
) : QuerySystem<MainCharacterComponent, PvPComponent>
{
    private readonly Stopwatch _timer = Stopwatch.StartNew();

    protected override void OnUpdate()
    {
        if (!areaState.CurrentArea.HasValue)
            return;

        if (_timer.Elapsed < TimeSpan.FromSeconds(1))
            return;

        _timer.Restart();

        List<string> redTeamList = [];
        List<string> blueTeamList = [];
        List<string> spectatorsList = [];

        Query
            .HasValue<InScopeComponent, Entity>(areaState.CurrentArea.Value.Entity)
            .ForEachEntity((ref MainCharacterComponent mainCharacterComponent, ref PvPComponent pvp, Entity _) =>
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

        widgetManager.SetTeams(redTeamList, blueTeamList, spectatorsList);
        widgetManager.RefreshWidgets();
    }
}