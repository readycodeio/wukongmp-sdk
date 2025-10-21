using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using System;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI;

public sealed class WukongWidgetManager : IDisposable
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
        _clientState.OnLeftArea += OnLeftArea;
        _clientState.OnConnected += OnConnected;
        _clientState.OnDisconnected += OnDisconnected;
        _clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;
        _eventBus.OnLevelLoaded += OnLevelLoaded;
        _eventBus.OnExitLevel += OnExitLevel;
    }

    public void Dispose()
    {
        _clientState.OnJoinedArea -= OnJoinedArea;
        _clientState.OnLeftArea -= OnLeftArea;
        _clientState.OnConnected -= OnConnected;
        _clientState.OnDisconnected -= OnDisconnected;
        _clientState.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;
        _eventBus.OnLevelLoaded -= OnLevelLoaded;
        _eventBus.OnExitLevel -= OnExitLevel;
    }

    public void UpdatePlayerTeam(PlayerEntity playerEntity, MainCharacterEntity mainCharacterEntity)
    {
        ref var playerComp = ref playerEntity.GetState();

        if (Constants.IsCoop)
        {
            CoopStatusWidget.Instance.RemovePlayer(playerComp.NickName);
            CoopStatusWidget.Instance.AddPlayer(playerComp.NickName);
        }
        else
        {
            var isSpectator = mainCharacterEntity.GetPvP().IsSpectator;
            LobbyStatusWidget.Instance.UpdatePlayerTeam(playerComp.NickName, playerComp.TeamId, isSpectator);
        }

        RefreshWidgets();
    }

    private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
    {
        var player = _playerState.GetPlayerById(playerId);
        if (player.HasValue)
        {
            var nickname = player.Value.GetState().NickName;
            if (Constants.IsCoop)
            {
                CoopStatusWidget.Instance.AddPlayer(nickname);
            }
            // else
            // {
            //     var main = _playerState.GetMainCharacterById(playerId);
            //     if (main.HasValue)
            //     {
            //         UpdatePlayerTeam(player.Value, main.Value);
            //     }
            // }

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
        GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(_clientState.AreaPlayers.Count, _playerState.LocalMainCharacter?.GetPvP().IsReadyForPvP == true));
    }

    public void ShowInGameWidgets()
    {
        if (Constants.IsCoop)
        {
            CoopStatusWidget.Instance.SetVisibility(true);
            CoopStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
        }
        else if (Constants.IsPvP)
        {
            LobbyStatusWidget.Instance.SetVisibility(true);
            LobbyStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        PingIndicatorWidget.Instance.SetVisibility(true);
        ChatWidget.Instance.SetVisibility(true);
    }

    private void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
        ModWidgetsUtils.SpawnWidgetManagerActor();
        ModWidgetsUtils.InitializeWidgets();
        ChatWidget.Instance.SetVisibility(false);

        if (!_clientState.IsConnected)
        {
            InfoMessageWidget.Instance.SetVisibility(true);
            InfoMessageWidget.Instance.SetText(Texts.Disconnected);
        }
    }

    private static void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing widgets");
        ModWidgetsUtils.DeinitializeWidgets();
    }

    private void OnJoinedArea(AreaId area, Entity areaEntity)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            CoopStatusWidget.Instance.AddPlayer(playerEntity.Value.GetState().NickName);
        }

        RefreshWidgets();
    }

    private void OnLeftArea(AreaId arg1, Entity arg2)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            CoopStatusWidget.Instance.RemovePlayer(playerEntity.Value.GetState().NickName);
        }

        RefreshWidgets();
    }

    private void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
    {
        InfoMessageWidget.Instance.SetVisibility(true);
        InfoMessageWidget.Instance.SetText(Texts.Disconnected);
    }

    private void OnConnected(PlayerId playerId, Entity entity)
    {
        InfoMessageWidget.Instance.SetVisibility(false);
    }
}