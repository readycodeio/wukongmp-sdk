using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems.PvP;

public sealed class PlayerListSystem(WukongPlayerState playerState) : QuerySystem<MainCharacterComponent, PvPComponent>
{
    private readonly Stopwatch _timer = Stopwatch.StartNew();

    protected override void OnUpdate()
    {
        if (_timer.Elapsed < TimeSpan.FromSeconds(1))
            return;

        _timer.Restart();

        Query.ForEachEntity((ref MainCharacterComponent mainCharacterComponent, ref PvPComponent pvp, Entity _) =>
        {
            var player = playerState.GetPlayerById(mainCharacterComponent.PlayerId);
            if (player.HasValue)
            {
                var team = player.Value.GetState().TeamId;
                LobbyStatusWidget.Instance.UpdatePlayerTeam(mainCharacterComponent.CharacterNickName, team, pvp.IsSpectator);
            }
        });

        LobbyStatusWidget.Instance.SetConnectedCount(Query.Count);
    }
}