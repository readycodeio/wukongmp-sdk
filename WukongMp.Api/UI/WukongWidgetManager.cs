using System;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.Api.UI;

public class WukongWidgetManager : IDisposable
{
    private readonly ClientState _clientState;
    private readonly WukongPlayerState _playerState;

    public WukongWidgetManager(ClientState pawnState, WukongPlayerState playerState)
    {
        _clientState = pawnState;
        _playerState = playerState;

        _clientState.OnJoinedArea += OnJoinedArea;
        _clientState.OnDisconnected += OnDisconnected;
        _clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;
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
    }

    public void Dispose()
    {
        _clientState.OnDisconnected -= OnDisconnected;
        _clientState.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;
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

    private void RefreshWidgets()
    {
        LobbyStatusWidget.Instance.SetConnectedCount(_clientState.AllPlayers.Count);
        CoopStatusWidget.Instance.SetConnectedCount(_clientState.AllPlayers.Count);
        CoopStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
        GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(_clientState.AllPlayers.Count, _playerState.LocalPlayerEntity?.GetState().IsReadyForPvP == true));
    }

    public void ShowInGameWidgets()
    {
        CoopStatusWidget.Instance.SetVisibility(true);
        CoopStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
        PingIndicatorWidget.Instance.SetVisibility(true);
        ChatWidget.Instance.SetVisibility(true);
    }
}