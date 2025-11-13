using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using System;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Coop.UI
{
    internal class CoopWidgetManager : IDisposable
    {
        private readonly ClientState _clientState;
        private readonly WukongPlayerState _playerState;

        private readonly CoopStatusWidget _coopStatusWidget = new();

        public CoopWidgetManager(ClientState clientState, WukongPlayerState playerState, WukongEventBus eventBus)
        {
            _clientState = clientState;
            _playerState = playerState;

            _clientState.OnJoinedArea += OnJoinedArea;
            _clientState.OnLeftArea += OnLeftArea;
            _clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
            _clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;
        }

        public void Dispose()
        {
            _clientState.OnJoinedArea -= OnJoinedArea;
            _clientState.OnLeftArea -= OnLeftArea;
            _clientState.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
            _clientState.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;
        }

        public void UpdatePlayerTeam(PlayerEntity playerEntity, MainCharacterEntity mainCharacterEntity)
        {
            ref var playerComp = ref playerEntity.GetState();

            _coopStatusWidget.RemovePlayer(playerComp.NickName);
            _coopStatusWidget.AddPlayer(playerComp.NickName);

            RefreshWidgets();
        }

        public void ShowInGameWidgets()
        {
            _coopStatusWidget.SetVisibility(true);
            _coopStatusWidget.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void InitializeWidgets()
        {
            _coopStatusWidget.Initialize();
        }

        private void DeinitializeWidgets()
        {
            _coopStatusWidget.Deinitialize();
        }

        public void RefreshWidgets()
        {
            _coopStatusWidget.SetConnectedCount(_clientState.AreaPlayers.Count);
            _coopStatusWidget.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
        {
            var player = _playerState.GetPlayerById(playerId);
            if (player.HasValue)
            {
                var nickname = player.Value.GetState().NickName;
                _coopStatusWidget.AddPlayer(nickname);
                RefreshWidgets();
            }
        }

        private void OnOtherPlayerOutsideArea(PlayerId arg1, AreaId arg2, OtherPlayerOutsideAreaReason arg3)
        {
            var player = _playerState.GetPlayerById(arg1);
            if (player.HasValue)
            {
                var nickname = player.Value.GetState().NickName;
                _coopStatusWidget.RemovePlayer(nickname);
                RefreshWidgets();
            }
        }

        private void OnJoinedArea(AreaId area, Entity areaEntity)
        {
            var playerEntity = _playerState.LocalPlayerEntity;
            if (playerEntity.HasValue)
            {
                _coopStatusWidget.AddPlayer(playerEntity.Value.GetState().NickName);
                RefreshWidgets();
            }
        }

        private void OnLeftArea(AreaId arg1, Entity arg2)
        {
            var playerEntity = _playerState.LocalPlayerEntity;
            if (playerEntity.HasValue)
            {
                _coopStatusWidget.RemovePlayer(playerEntity.Value.GetState().NickName);
                RefreshWidgets();
            }
        }
    }
}
