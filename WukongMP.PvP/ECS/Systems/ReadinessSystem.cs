using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.GameMode;
using WukongMp.PvP.Resources;
using WukongMp.PvP.UI;

namespace WukongMp.PvP.ECS.Systems;

internal sealed class ReadinessSystem(
    Store world,
    WukongAreaState areaState,
    PvpWidgetManager widgetManager,
    WukongPlayerState playerState,
    PvpMode pvpMode
) : QuerySystem<PvPComponent, TeamComponent, InScopeComponent>
{
    private int _lastPlayers = -1;
    private int _lastReadyCount = -1;

    protected override void OnUpdate()
    {
        if (!areaState.CurrentArea.HasValue)
            return;

        if (areaState.PvpState.HasValue && areaState.PvpState.Value.InTournament)
            return;

        var players = 0;
        var readyCount = 0;
        var blueTeamAnyReady = false;
        var redTeamAnyReady = false;

        Query.ForEachEntity((ref pvp, ref team, ref scope, _) =>
        {
            if (scope.ScopeEntity != areaState.CurrentArea.Value.Entity)
                return;

            if (pvp.IsObserver)
                return;

            players++;
            if (pvp.IsReadyForPvP)
            {
                readyCount++;
                switch (team.TeamId)
                {
                    case PvpConstants.BlueTeamId:
                        blueTeamAnyReady = true;
                        break;
                    case PvpConstants.RedTeamId:
                        redTeamAnyReady = true;
                        break;
                }
            }
        });

        if (_lastReadyCount == readyCount && _lastPlayers == players)
            return;

        _lastReadyCount = readyCount;
        _lastPlayers = players;

        widgetManager.UpdateReadyCount(readyCount, players);

        if (areaState.PvpState is { InPvP: false })
        {
            var allReady = readyCount == players && players > 0;

            world.Query<LocalTamerComponent, TeamComponent>().ForEachEntity((ref LocalTamerComponent localTamerComp, ref TeamComponent team, Entity _) =>
            {
                if (localTamerComp.IsTamerSynced)
                {
                    switch (team.TeamId)
                    {
                        case PvpConstants.BlueTeamId:
                            blueTeamAnyReady = true;
                            break;
                        case PvpConstants.RedTeamId:
                            redTeamAnyReady = true;
                            break;
                    }
                }
            });

            var isSpectator = playerState.LocalMainCharacter!.Value.GetPvP().IsSpectator;
            if (allReady)
            {
                if (blueTeamAnyReady && redTeamAnyReady)
                {
                    pvpMode.StartLobbyCountdown(PvpConstants.CountdownSeconds);
                }
                else if (!isSpectator)
                {
                    // show a message that both teams need at least one ready player
                    widgetManager.SetThirdText(PvpTexts.BothTeamsNeedReadyPlayers);
                }
            }
            else
            {
                pvpMode.CancelLobbyCountdown();
            }
        }
    }
}