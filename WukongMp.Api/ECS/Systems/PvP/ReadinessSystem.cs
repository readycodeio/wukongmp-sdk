using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.PVP;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems.PvP;

public class ReadinessSystem(WukongAreaState areaState, WukongPVP pvpUtils) : QuerySystem<PvPComponent>
{
    private int _lastReadyCount = -1;

    protected override void OnUpdate()
    {
        var players = 0;
        var readyCount = 0;

        Query.ForEachEntity((ref PvPComponent pvp, Entity entity) =>
        {
            players++;
            if (pvp.IsReadyForPvP)
                readyCount++;
        });

        if (_lastReadyCount == readyCount)
            return;

        _lastReadyCount = readyCount;

        var allReady = readyCount == players && players > 0;

        if (allReady && (players > 1 || areaState.CurrentArea?.GetRoom().BotsEnabled == true))
        {
            // all players are ready
            GameMessageWidget.Instance.SetMainText(Texts.StartingGame);
            CountdownWidget.Instance.StartLobbyCountdown(Constants.CountdownSeconds, pvpUtils.StartPvP);
        }
        else
        {
            CountdownWidget.Instance.StopCountdown();
            GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
        }

        LobbyStatusWidget.Instance.SetReadyCount(readyCount);
    }
}