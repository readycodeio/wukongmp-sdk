using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.State;
using WukongMp.PvP.Configuration;
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
            .ForEachEntity((ref mainComp, ref pvp, _) =>
            {
                var player = playerState.GetPlayerById(mainComp.PlayerId);
                if (player.HasValue)
                {
                    var team = player.Value.GetState().TeamId;

                    switch (team)
                    {
                        case PvpConstants.RedTeamId:
                            redTeamList.Add(mainComp.CharacterNickName);
                            return;
                        case PvpConstants.BlueTeamId:
                            blueTeamList.Add(mainComp.CharacterNickName);
                            return;
                        case PvpConstants.SpectatorTeamId:
                            spectatorsList.Add(mainComp.CharacterNickName);
                            return;
                    }
                }
            });

        widgetManager.SetTeams(redTeamList, blueTeamList, spectatorsList);
        widgetManager.RefreshWidgets();
    }
}