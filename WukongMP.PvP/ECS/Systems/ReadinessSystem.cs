using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;
using WukongMp.PvP.Gamemode;
using WukongMp.PvP.UI;

namespace WukongMp.PvP.ECS.Systems;

internal sealed class ReadinessSystem(
    WukongAreaState areaState,
    PvpWidgetManager widgetManager,
    PvpMode pvpMode
) : QuerySystem<PvPComponent, InScopeComponent>
{
    private int _lastReadyCount = -1;

    protected override void OnUpdate()
    {
        if (!areaState.CurrentArea.HasValue)
            return;

        var players = 0;
        var readyCount = 0;

        Query.ForEachEntity((ref pvp, ref scope, _) =>
        {
            if (scope.ScopeEntity != areaState.CurrentArea.Value.Entity)
                return;

            players++;
            if (pvp.IsReadyForPvP)
                readyCount++;
        });

        if (_lastReadyCount == readyCount)
            return;

        _lastReadyCount = readyCount;
        widgetManager.UpdateReadyCount(readyCount);

        if (areaState.PvpState is { InPvP: false })
        {
            var allReady = readyCount == players && players > 0;
            if (allReady)
            {
                pvpMode.StartLobbyCountdown(Constants.CountdownSeconds);
            }
            else
            {
                pvpMode.CancelLobbyCountdown();
            }
        }
    }
}