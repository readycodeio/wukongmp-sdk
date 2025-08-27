using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using System;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI;

public class WukongWidgetManager : IDisposable
{
    private readonly ClientState _clientState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongEventBus _eventBus;

    public WukongWidgetManager(ClientState pawnState, WukongPlayerState playerState, WukongEventBus eventBus)
    {
        _clientState = pawnState;
        _playerState = playerState;
        _eventBus = eventBus;

        _clientState.OnJoinedArea += OnJoinedArea;
        _clientState.OnConnected +=OnConnected; ;
        _clientState.OnDisconnected += OnDisconnected;
        _clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;
        _eventBus.OnLevelLoaded += OnLevelLoaded;
        _eventBus.OnExitLevel += OnExitLevel;
    }

    public void Dispose()
    {
        _clientState.OnConnected -= OnConnected;
        _clientState.OnDisconnected -= OnDisconnected;
        _clientState.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;
        _eventBus.OnLevelLoaded -= OnLevelLoaded;
        _eventBus.OnExitLevel -= OnExitLevel;
    }

    public void UpdatePlayerTeam(PlayerEntity playerEntity)
    {
        ref var playerComp = ref playerEntity.GetState();

        if (Constants.IsCoop)
        {
            CoopStatusWidget.Instance.RemovePlayer(playerComp.NickName);
            CoopStatusWidget.Instance.AddPlayer(playerComp.NickName);
        }
        else
        {
            LobbyStatusWidget.Instance.UpdatePlayerTeam(playerComp.NickName, playerComp.TeamId, playerComp.IsSpectator);
        }

        RefreshWidgets();
    }

    private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
    {
        var player = _playerState.GetPlayerById(playerId);
        if (player.HasValue)
        {
            var nickname = player.Value.GetState().NickName;
            CoopStatusWidget.Instance.AddPlayer(nickname);
            RefreshWidgets();
        }
    }

    private void OnOtherPlayerOutsideArea(PlayerId arg1, AreaId arg2, OtherPlayerOutsideAreaReason arg3)
    {
        var player = _playerState.GetPlayerById(arg1);
        if (player.HasValue)
        {
            var nickname = player.Value.GetState().NickName;
            CoopStatusWidget.Instance.RemovePlayer(nickname);
            RefreshWidgets();
        }
    }

    public void RefreshWidgets()
    {
        LobbyStatusWidget.Instance.SetConnectedCount(_clientState.AreaPlayers.Count);
        CoopStatusWidget.Instance.SetConnectedCount(_clientState.AreaPlayers.Count);
        CoopStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
        GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(_clientState.AreaPlayers.Count, _playerState.LocalPlayerEntity?.GetState().IsReadyForPvP == true));
    }

    public void ShowInGameWidgets()
    {
        CoopStatusWidget.Instance.SetVisibility(true);
        CoopStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
        PingIndicatorWidget.Instance.SetVisibility(true);
        ChatWidget.Instance.SetVisibility(true);
    }

    private void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
        ModWidgetsUtils.SpawnWidgetManagerActor();
        ModWidgetsUtils.InitializeWidgets();
        ChatWidget.Instance.SetVisibility(false);
    }

    private void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing widgets");
        ModWidgetsUtils.DeinitializeWidgets();
    }

    private void OnJoinedArea(AreaId arg1, Entity arg2)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            CoopStatusWidget.Instance.AddPlayer(playerEntity.Value.GetState().NickName);
        }

        RefreshWidgets();
    }

    private void OnDisconnected(PlayerId playerId, Entity entity, DisconnectReason reason)
    {
        var nickname = new PlayerEntity(entity).GetState().NickName;
        CoopStatusWidget.Instance.RemovePlayer(nickname);
        RefreshWidgets();

        InfoMessageWidget.Instance.SetVisibility(true);
        InfoMessageWidget.Instance.SetText("Disconnected");
    }

    private void OnConnected(PlayerId playerId, Entity entity)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            CoopStatusWidget.Instance.AddPlayer(playerEntity.Value.GetState().NickName);
        }
        RefreshWidgets();
        InfoMessageWidget.Instance.SetVisibility(false);
    }
}
