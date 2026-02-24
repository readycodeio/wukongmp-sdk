using System;
using System.Collections.Generic;
using B1UI;
using B1UI.GSUI;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Relay.Client.State;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.PvP.UI
{
    internal class PvpWidgetManager
    {
        public readonly WukongWidgetManager WidgetManager;
        
        private readonly ClientState _clientState;
        private readonly WukongPlayerState _playerState;
        private readonly WukongEventBus _eventBus;
        private readonly FreeCameraManager _freeCameraManager;
        private readonly WukongAreaState _areaState;
        private readonly GameplayEventRouter _eventRouter;

        private readonly Lazy<LobbyStatusWidget> _lobbyStatusWidget = new();
        private readonly Lazy<GameMessageWidget> _gameMessageWidget = new();
        private readonly Lazy<CountdownWidget> _countdownWidget = new();

        private bool _isAfterLoadingScreen;

        public PvpWidgetManager(WukongWidgetManager widgetManager, ClientState clientState, WukongPlayerState playerState, WukongEventBus eventBus, FreeCameraManager freeCameraManager, WukongAreaState areaState, GameplayEventRouter eventRouter)
        {
            WidgetManager = widgetManager;
            _clientState = clientState;
            _playerState = playerState;
            _eventBus = eventBus;
            _freeCameraManager = freeCameraManager;
            _areaState = areaState;
            _eventRouter = eventRouter;

            _clientState.OnJoinedArea += OnJoinedArea;
            _clientState.OnLeftArea += OnLeftArea;
            _clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
            _clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;

            _clientState.OnConnected += OnConnected;
            _clientState.OnDisconnected += OnDisconnected;
            _eventBus.OnLevelLoaded += OnLevelLoaded;
            _eventBus.OnExitLevel += OnExitLevel;
            _eventBus.OnLoadingScreenClose += OnLoadingScreenClose;

            _freeCameraManager.OnFreeCameraModeChanged += OnFreeCameraModeChanged;

            _eventRouter.OnPlayerChangedTeam += UpdatePlayerTeam;
            _eventRouter.OnLocalPlayerChangedSpectator += OnLocalPlayerChangedSpectator;
        }

        public void Dispose()
        {
            _clientState.OnJoinedArea -= OnJoinedArea;
            _clientState.OnLeftArea -= OnLeftArea;
            _clientState.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
            _clientState.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;

            _clientState.OnConnected -= OnConnected;
            _clientState.OnDisconnected -= OnDisconnected;
            _eventBus.OnLevelLoaded -= OnLevelLoaded;
            _eventBus.OnExitLevel -= OnExitLevel;
            _eventBus.OnLoadingScreenClose -= OnLoadingScreenClose;

            _freeCameraManager.OnFreeCameraModeChanged -= OnFreeCameraModeChanged;

            _eventRouter.OnPlayerChangedTeam -= UpdatePlayerTeam;
            _eventRouter.OnLocalPlayerChangedSpectator -= OnLocalPlayerChangedSpectator;
        }

        public void UpdatePlayerTeam(PlayerEntity playerEntity, MainCharacterEntity mainCharacterEntity)
        {
            ref var playerComp = ref playerEntity.GetState();

            _lobbyStatusWidget.Value.UpdatePlayerTeam(playerComp.NickName, playerComp.TeamId);
            RefreshWidgets();
        }

        public void SetMainMessage(string message)
        {
            _gameMessageWidget.Value.SetMainText(message);
        }

        public void SetThirdText(string message)
        {
            _gameMessageWidget.Value.SetThirdText(message);
        }

        public void UpdateRoundCountdown(int minutesLeft, int secondsLeft)
        {
            _countdownWidget.Value.SetText(secondsLeft);
        }

        public void ShowCountdown()
        {
            _countdownWidget.Value.SetVisibility(true);
        }

        public void HideCountdown()
        {
            _countdownWidget.Value.SetVisibility(false);
        }

        private void ShowInGameWidgets()
        {
            _lobbyStatusWidget.Value.SetVisibility(true);
            _lobbyStatusWidget.Value.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void OnLevelLoaded()
        {
            WidgetManager.OnLevelLoaded();

            Logging.LogDebug("Initializing pvp widgets");
            InitializeWidgets();
        }

        private void OnExitLevel()
        {
            Logging.LogDebug("Deinitializing pvp widgets");
            DeinitializeWidgets();

            WidgetManager.OnExitLevel();
            _isAfterLoadingScreen = false;
        }

        private void OnLoadingScreenClose()
        {
            var isOnGameplayLevel = _areaState.CurrentArea != null;
            WidgetManager.ShowInGameWidgets(isOnGameplayLevel);
            if (isOnGameplayLevel)
            {
                ShowInGameWidgets();
                _isAfterLoadingScreen = true;

                if (_playerState.LocalMainCharacter?.GetPvP().IsSpectator == false)
                {
                    SetupLobbyUi();
                }
                else
                {
                    SetupSpectatorUi();
                }
            }
        }

        private void OnConnected(PlayerId playerId, Entity entity)
        {
            WidgetManager.OnConnected(playerId, entity);
        }

        private void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
        {
            WidgetManager.OnDisconnected(playerId, entity, reason);
        }

        private void OnFreeCameraModeChanged(bool enabled)
        {
            WidgetManager.OnFreeCameraModeChanged(enabled);
        }

        private void OnLocalPlayerChangedSpectator(bool enabled)
        {
            if (enabled)
            {
                SetupSpectatorUi();
            }
            else
            {
                if (_areaState.PvpState is not { InPvP: true })
                {
                    SetupLobbyUi();
                }
                else
                {
                    _lobbyStatusWidget.Value.SetVisibility(false);
                }
            }
        }

        private void InitializeWidgets()
        {
            _lobbyStatusWidget.Value.Initialize();
            _gameMessageWidget.Value.Initialize();
            _countdownWidget.Value.Initialize();
        }

        private void DeinitializeWidgets()
        {
            _lobbyStatusWidget.Value.Deinitialize();
            _gameMessageWidget.Value.Deinitialize();
            _countdownWidget.Value.Deinitialize();
        }

        public void RefreshWidgets()
        {
            _lobbyStatusWidget.Value.SetConnectedCount(_clientState.AreaPlayers.Count);
        }

        public void StartRound()
        {
            _gameMessageWidget.Value.SetVisibility(false);
            if (GSG.GSPageOP.FindUIPage(12) != null)
                GSB1UIUtil.ExitEquipScene(GameUtils.GetWorld());
        }

        public void SwitchReadyState(bool isReady)
        {
            _gameMessageWidget.Value.SetThirdText(isReady ? Texts.YouAreReady : Texts.PressToSwitchTeam);
            _gameMessageWidget.Value.SetSecondText(TextUtils.GetReadyText(_clientState.AllPlayers.Count, isReady));
        }

        public void UpdateReadyCount(int readyCount, int maxCount)
        {
            _lobbyStatusWidget.Value.SetReadyCount(readyCount, maxCount);
        }

        public void SetTeams(List<string> redTeamList, List<string> blueTeamList, List<string> spectatorsList) => _lobbyStatusWidget.Value.SetTeams(redTeamList, blueTeamList, spectatorsList);
        
        public void SetupLobbyUi()
        {
            if (!_isAfterLoadingScreen)
                return;

            _gameMessageWidget.Value.SetVisibility(true);
            _gameMessageWidget.Value.SetMainText(Texts.InMultiplayer);
            _gameMessageWidget.Value.SetSecondText(TextUtils.GetReadyText(_clientState.AllPlayers.Count, _playerState.LocalMainCharacter?.GetPvP().IsReadyForPvP == true));
            _gameMessageWidget.Value.SetThirdText(Texts.PressToSwitchTeam);
            _lobbyStatusWidget.Value.SetVisibility(true);
        }

        public void SetupSpectatorUi()
        {
            if (!_isAfterLoadingScreen)
                return;

            _gameMessageWidget.Value.SetVisibility(true);
            _gameMessageWidget.Value.SetMainText(Texts.InMultiplayer);
            _gameMessageWidget.Value.SetSecondText(Texts.WaitForEnd);
            _gameMessageWidget.Value.SetThirdText("");
            _lobbyStatusWidget.Value.SetVisibility(true);
        }

        private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
        {
            WidgetManager.OnOtherPlayerInsideArea(playerId, area, reason);
            RefreshWidgets();
        }

        private void OnOtherPlayerOutsideArea(PlayerId arg1, AreaId arg2, OtherPlayerOutsideAreaReason arg3)
        {
            WidgetManager.OnOtherPlayerOutsideArea(arg1, arg2, arg3);
            RefreshWidgets();
        }

        private void OnJoinedArea(AreaId area, Entity areaEntity)
        {
            WidgetManager.OnJoinedArea(area, areaEntity);
            RefreshWidgets();
        }

        private void OnLeftArea(AreaId arg1, Entity arg2)
        {
            WidgetManager.OnLeftArea(arg1, arg2);
            RefreshWidgets();
        }
    }
}