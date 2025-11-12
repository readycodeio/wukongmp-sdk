using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.PvP.Gamemode;

namespace WukongMp.PvP.ECS.Systems;

public sealed class ReadinessSystem(
    WukongAreaState areaState,
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

        Query.ForEachEntity((ref PvPComponent pvp, ref InScopeComponent scope, Entity _) =>
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
        LobbyStatusWidget.Instance.SetReadyCount(readyCount);

        if (areaState.PvpState is { InPvP: false })
        {
            var allReady = readyCount == players && players > 0;
            if (allReady)
            {
                // all players are ready
                GameMessageWidget.Instance.SetMainText(Texts.StartingGame);
                CountdownWidget.Instance.StartLobbyCountdown(Constants.CountdownSeconds, pvpMode.StartPvP);
            }
            else
            {
                CountdownWidget.Instance.StopCountdown();
                GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
            }
        }
    }
}