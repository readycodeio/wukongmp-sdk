using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.PvP.UI
{
    internal class PvpWidgetManager
    {
        private readonly ClientState _clientState;
        private readonly WukongPlayerState _playerState;
        private readonly WukongWidgetManager _widgetManager;

        private readonly LobbyStatusWidget _lobbyStatusWidget = new();
        private readonly GameMessageWidget _gameMessageWidget = new();
        private readonly CountdownWidget _countdownWidget = new();

        public PvpWidgetManager(WukongWidgetManager widgetManager, ClientState clientState, WukongPlayerState playerState)
        {
            _widgetManager = widgetManager;
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

            var isSpectator = mainCharacterEntity.GetPvP().IsSpectator;
            _lobbyStatusWidget.UpdatePlayerTeam(playerComp.NickName, playerComp.TeamId, isSpectator);
            RefreshWidgets();
        }

        // TODO: call these
        public void ShowInGameWidgets()
        {
            _lobbyStatusWidget.SetVisibility(true);
            _lobbyStatusWidget.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void InitializeWidgets()
        {
            _lobbyStatusWidget.Initialize();
            _gameMessageWidget.Initialize();
            _countdownWidget.Initialize();
        }

        private void DeinitializeWidgets()
        {
            _lobbyStatusWidget.Deinitialize();
            _gameMessageWidget.Deinitialize();
            _countdownWidget.Deinitialize();
        }

        public void RefreshWidgets()
        {
            _lobbyStatusWidget.SetConnectedCount(_clientState.AreaPlayers.Count);
        }

        public void StartRound()
        {
            _gameMessageWidget.SetVisibility(false);
            _countdownWidget.StopCountdown();
        }

        public void SwitchReadyState(bool isReady)
        {
            _gameMessageWidget.SetThirdText(isReady ? Texts.YouAreReady : Texts.PressToSwitchTeam);
            _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(_clientState.AllPlayers.Count, isReady));
        }

        public void UpdateReadyCount(int readyCount)
        {
            _lobbyStatusWidget.SetReadyCount(readyCount);
        }

        private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
        {
            RefreshWidgets();
        }

        private void OnOtherPlayerOutsideArea(PlayerId arg1, AreaId arg2, OtherPlayerOutsideAreaReason arg3)
        {
            RefreshWidgets();
        }

        private void OnJoinedArea(AreaId area, Entity areaEntity)
        {
            RefreshWidgets();
        }

        private void OnLeftArea(AreaId arg1, Entity arg2)
        {
            RefreshWidgets();
        }
    }
}
