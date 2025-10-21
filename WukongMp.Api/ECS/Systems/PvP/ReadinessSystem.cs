using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
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
    private readonly ArchetypeEventRouter _archetypeEvent;
    private readonly ClientWukongArchetypeRegistration _wukongArchetype;
    private readonly WukongWidgetManager _widgetManager;
    private readonly WukongPlayerState _playerState;

    public ReadinessSystem(
        WukongAreaState areaState,
        WukongPVP pvpUtils,
        ArchetypeEventRouter archetypeEvent,
        ClientWukongArchetypeRegistration wukongArchetype,
        WukongWidgetManager widgetManager,
        WukongPlayerState playerState)
    {
        _areaState = areaState;
        _pvpUtils = pvpUtils;
        _archetypeEvent = archetypeEvent;
        _wukongArchetype = wukongArchetype;
        _widgetManager = widgetManager;
        _playerState = playerState;

        archetypeEvent[_wukongArchetype.MainCharacterArchetype].OnEntityCreate += OnMainCharacterCreate;
    }

    private void OnMainCharacterCreate(EntityCreate obj)
    {
        var main = new MainCharacterEntity(obj.Entity);
        var id = main.GetState().PlayerId;
        var player = _playerState.GetPlayerById(id);
        if (player.HasValue)
        {
            _widgetManager.UpdatePlayerTeam(player.Value, main);
        }
    }

    public void Dispose()
    {
        _archetypeEvent[_wukongArchetype.MainCharacterArchetype].OnEntityCreate -= OnMainCharacterCreate;
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