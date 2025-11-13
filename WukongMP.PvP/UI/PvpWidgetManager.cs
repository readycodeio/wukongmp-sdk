using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.PvP.Gamemode;

namespace WukongMp.PvP.UI
{
    internal class PvpWidgetManager
    {
        private readonly ClientState _clientState;
        public readonly WukongWidgetManager widgetManager;
        private readonly WukongEventBus _eventBus;
        private readonly FreeCameraManager _freeCameraManager;
        private readonly WukongAreaState _areaState;
        private readonly GameplayEventRouter _eventRouter;

        private readonly LobbyStatusWidget _lobbyStatusWidget = new();
        private readonly GameMessageWidget _gameMessageWidget = new();
        private readonly CountdownWidget _countdownWidget = new();

        public PvpWidgetManager(WukongWidgetManager widgetManager, ClientState clientState, WukongEventBus eventBus, FreeCameraManager freeCameraManager, WukongAreaState areaState, GameplayEventRouter eventRouter)
        {
            this.widgetManager = widgetManager;
            _clientState = clientState;
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
        }

        public void UpdatePlayerTeam(PlayerEntity playerEntity, MainCharacterEntity mainCharacterEntity)
        {
            ref var playerComp = ref playerEntity.GetState();

            var isSpectator = mainCharacterEntity.GetPvP().IsSpectator;
            _lobbyStatusWidget.UpdatePlayerTeam(playerComp.NickName, playerComp.TeamId, isSpectator);
            RefreshWidgets();
        }

        public void SetupPvpLobby(bool allReady)
        {
            if (allReady)
            {
                _gameMessageWidget.SetMainText(Texts.StartingGame);
                _countdownWidget.StartLobbyCountdown(Constants.CountdownSeconds, pvpMode.StartPvP);
            }
            else
            {
                _countdownWidget.StopCountdown();
                _gameMessageWidget.SetMainText(Texts.InMultiplayer);
            }
        }

        private void ShowInGameWidgets()
        {
            _lobbyStatusWidget.SetVisibility(true);
            _lobbyStatusWidget.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void OnLevelLoaded()
        {
            widgetManager.OnLevelLoaded();

            Logging.LogDebug("Initializing pvp widgets");
            InitializeWidgets();
        }

        private void OnExitLevel()
        {
            Logging.LogDebug("Deinitializing pvp widgets");
            DeinitializeWidgets();

            widgetManager.OnExitLevel();
        }

        private void OnLoadingScreenClose()
        {
            if (_areaState.CurrentArea != null)
            {
                widgetManager.ShowInGameWidgets();
                ShowInGameWidgets();
            }
        }

        private void OnConnected(PlayerId playerId, Entity entity)
        {
            widgetManager.OnConnected(playerId, entity);
        }

        private void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
        {
            widgetManager.OnDisconnected(playerId, entity, reason);
        }

        private void OnFreeCameraModeChanged(bool enabled)
        {
            widgetManager.OnFreeCameraModeChanged(enabled);
            if (!enabled && _areaState.PvpState is { InPvP: true })
            {
                _lobbyStatusWidget.SetVisibility(false);
            }
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
