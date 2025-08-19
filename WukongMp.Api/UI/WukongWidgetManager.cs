using System;
using Friflo.Engine.ECS;
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

        _clientState.OnJoinedArea += AreaChangeHandler;
        _clientState.OnLeftArea += AreaChangeHandler;
        _clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;
    }

    public void Dispose()
    {
        _clientState.OnJoinedArea -= AreaChangeHandler;
    }
    
    public void UpdatePlayerTeamUi(PlayerEntity playerEntity)
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

    private void AreaChangeHandler(AreaId arg1, Entity arg2) => RefreshWidgets();
    private void OnOtherPlayerInsideArea(PlayerId arg1, AreaId arg2, OtherPlayerInsideAreaReason arg3) => RefreshWidgets();
    private void OnOtherPlayerOutsideArea(PlayerId arg1, AreaId arg2, OtherPlayerOutsideAreaReason arg3) => RefreshWidgets();

    private void RefreshWidgets()
    {
        LobbyStatusWidget.Instance.SetConnectedCount(_clientState.AllPlayers.Count);
        CoopStatusWidget.Instance.SetConnectedCount(_clientState.AllPlayers.Count);
        CoopStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
        GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(_clientState.AllPlayers.Count, _playerState.LocalPlayerEntity?.GetState().IsReadyForPvP == true));
    }
}