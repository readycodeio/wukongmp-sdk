using Friflo.Engine.ECS.Systems;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Gamemode;
using WukongMp.PvP.UI;

namespace WukongMp.PvP.ECS.Systems;

internal sealed class ReadinessSystem(
    Store world,
    WukongAreaState areaState,
    PvpWidgetManager widgetManager,
    PvpMode pvpMode
) : QuerySystem<PvPComponent, TeamComponent, InScopeComponent>
{
    private int _lastReadyCount = -1;

    protected override void OnUpdate()
    {
        if (!areaState.CurrentArea.HasValue)
            return;

        var players = 0;
        var readyCount = 0;
        var blueTeamAnyReady = false;
        var redTeamAnyReady = false;

        Query.ForEachEntity((ref pvp, ref team, ref scope, _) =>
        {
            if (scope.ScopeEntity != areaState.CurrentArea.Value.Entity)
                return;

            if (pvp.IsSpectator)
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

        if (_lastReadyCount == readyCount)
            return;

        _lastReadyCount = readyCount;
        widgetManager.UpdateReadyCount(readyCount, players);

        if (areaState.PvpState is { InPvP: false })
        {
            var allReady = readyCount == players && players > 0;

            world.Query<LocalTamerComponent, TeamComponent>().ForEachEntity((ref localTamerComp, ref team, _) =>
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

            if (allReady)
            {
                if (blueTeamAnyReady && redTeamAnyReady)
                {
                    pvpMode.StartLobbyCountdown(PvpConstants.CountdownSeconds);
                }
                else
                {
                    // show a message that both teams need at least one ready player
                    widgetManager.SetThirdText(Resources.PvpTexts.BothTeamsNeedReadyPlayers);
                }
            }
            else
            {
                pvpMode.CancelLobbyCountdown();
            }
        }
    }
}