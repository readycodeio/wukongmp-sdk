using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.PVP;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems.PvP;

public sealed class ReadinessSystem : QuerySystem<PvPComponent>, IDisposable
{
    private int _lastReadyCount = -1;
    private readonly WukongAreaState _areaState;
    private readonly WukongPVP _pvpUtils;
    private readonly NetworkedEntityManager _netEntity;
    private readonly WukongWidgetManager _widgetManager;
    private readonly WukongPlayerState _playerState;

    public ReadinessSystem(
        WukongAreaState areaState,
        WukongPVP pvpUtils,
        NetworkedEntityManager netEntity,
        WukongWidgetManager widgetManager,
        WukongPlayerState playerState)
    {
        _areaState = areaState;
        _pvpUtils = pvpUtils;
        _netEntity = netEntity;
        _widgetManager = widgetManager;
        _playerState = playerState;

        _netEntity.OnRemoteEntityCreated += OnRemoteEntityCreated;
    }

    private void OnRemoteEntityCreated(Entity entity)
    {
        if (entity.TryGetComponent<MainCharacterComponent>(out var main))
        {
            var id = main.PlayerId;
            var player = _playerState.GetPlayerById(id);
            if (player.HasValue)
            {
                _widgetManager.UpdatePlayerTeam(player.Value, new MainCharacterEntity(entity));
            }
        }
    }

    public void Dispose()
    {
        _netEntity.OnRemoteEntityCreated -= OnRemoteEntityCreated;
    }

    protected override void OnUpdate()
    {
        var players = 0;
        var readyCount = 0;

        Query.ForEachEntity((ref PvPComponent pvp, Entity _) =>
        {
            players++;
            if (pvp.IsReadyForPvP)
                readyCount++;
        });

        if (_lastReadyCount == readyCount)
            return;

        _lastReadyCount = readyCount;

        var allReady = readyCount == players && players > 0;

        if (allReady && (players > 1 || _areaState.CurrentArea?.GetRoom().BotsEnabled == true))
        {
            // all players are ready
            GameMessageWidget.Instance.SetMainText(Texts.StartingGame);
            CountdownWidget.Instance.StartLobbyCountdown(Constants.CountdownSeconds, _pvpUtils.StartPvP);
        }
        else
        {
            CountdownWidget.Instance.StopCountdown();
            GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
        }

        LobbyStatusWidget.Instance.SetReadyCount(readyCount);
    }
}