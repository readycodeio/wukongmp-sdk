using Friflo.Engine.ECS;
using LiteNetLib;
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
        private readonly WukongEventBus _eventBus;
        private readonly FreeCameraManager _freeCameraManager;
        private readonly WukongWidgetManager _widgetManager;
        private readonly WukongAreaState _areaState;
        private readonly GameplayEventRouter _eventRouter;

        private readonly CoopStatusWidget _coopStatusWidget = new();

        public CoopWidgetManager(WukongWidgetManager widgetManager, ClientState clientState, WukongPlayerState playerState, WukongEventBus eventBus, FreeCameraManager freeCameraManager, WukongAreaState areaState, GameplayEventRouter eventRouter)
        {
            _widgetManager = widgetManager;
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

            _coopStatusWidget.RemovePlayer(playerComp.NickName);
            _coopStatusWidget.AddPlayer(playerComp.NickName);

            RefreshWidgets();
        }

        public void ShowInGameWidgets()
        {
            _coopStatusWidget.SetVisibility(true);
            _coopStatusWidget.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void OnLevelLoaded()
        {
            _widgetManager.OnLevelLoaded();

            Logging.LogDebug("Initializing pvp widgets");
            InitializeWidgets();
        }

        private void OnExitLevel()
        {
            Logging.LogDebug("Deinitializing pvp widgets");
            DeinitializeWidgets();

            _widgetManager.OnExitLevel();
        }

        private void OnLoadingScreenClose()
        {
            if (_areaState.CurrentArea != null)
            {
                _widgetManager.ShowInGameWidgets();
                ShowInGameWidgets();
            }
        }

        private void OnConnected(PlayerId playerId, Entity entity)
        {
            _widgetManager.OnConnected(playerId, entity);
        }

        private void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
        {
            _widgetManager.OnDisconnected(playerId, entity, reason);
        }

        private void OnFreeCameraModeChanged(bool enabled)
        {
            _widgetManager.OnFreeCameraModeChanged(enabled);
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
