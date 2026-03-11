using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Relay.Client.State;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Coop.Patches
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

        private readonly Lazy<CoopStatusWidget> _coopStatusWidget = new();

        internal CoopWidgetManager(WukongWidgetManager widgetManager, ClientState clientState, WukongPlayerState playerState, WukongEventBus eventBus, FreeCameraManager freeCameraManager, WukongAreaState areaState, GameplayEventRouter eventRouter)
        {
            _widgetManager = widgetManager;
            _clientState = clientState;
            _playerState = playerState;
            _eventBus = eventBus;
            _freeCameraManager = freeCameraManager;
            _areaState = areaState;
            _eventRouter = eventRouter;
        }

        internal void Initialize()
        {
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
            _eventRouter.OnLocalPlayerBeforeRebirth += OnLocalPlayerBeforeRebirth;
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
            _eventRouter.OnLocalPlayerBeforeRebirth -= OnLocalPlayerBeforeRebirth; ;
        }

        private void UpdatePlayerTeam(PlayerEntity playerEntity, MainCharacterEntity mainCharacterEntity)
        {
            ref var playerComp = ref playerEntity.GetState();

            _coopStatusWidget.Value.RemovePlayer(playerComp.NickName);
            _coopStatusWidget.Value.AddPlayer(playerComp.NickName);

            RefreshWidgets();
        }

        private void ShowInGameWidgets()
        {
            _coopStatusWidget.Value.SetVisibility(true);
            _coopStatusWidget.Value.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void OnLevelLoaded()
        {
            _widgetManager.OnLevelLoaded();

            Logging.LogDebug("Initializing co-op widgets");
            InitializeWidgets();
        }

        private void OnExitLevel()
        {
            Logging.LogDebug("Deinitializing co-op widgets");
            DeinitializeWidgets();

            _widgetManager.OnExitLevel();
        }

        private void OnLoadingScreenClose()
        {
            bool isOnGameplayLevel = _areaState.CurrentArea != null;
            _widgetManager.ShowInGameWidgets(isOnGameplayLevel);
            if (isOnGameplayLevel)
            {
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
            _coopStatusWidget.Value.Initialize();
        }

        private void DeinitializeWidgets()
        {
            _coopStatusWidget.Value.Deinitialize();
        }

        internal void RefreshWidgets()
        {
            _coopStatusWidget.Value.SetConnectedCount(_clientState.AreaPlayers.Count);
            _coopStatusWidget.Value.SetMaxConnectedCount(Constants.MaxPlayers);
        }

        private void OnLocalPlayerBeforeRebirth()
        {
            _widgetManager.HideInfoMessage();
        }

        private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
        {
            _widgetManager.OnOtherPlayerInsideArea(playerId, area, reason);
            var player = _playerState.GetPlayerById(playerId);
            if (player.HasValue)
            {
                var nickname = player.Value.GetState().NickName;
                _coopStatusWidget.Value.AddPlayer(nickname);
                RefreshWidgets();
            }
        }

        private void OnOtherPlayerOutsideArea(PlayerId arg1, AreaId arg2, OtherPlayerOutsideAreaReason arg3)
        {
            _widgetManager.OnOtherPlayerOutsideArea(arg1, arg2, arg3);
            var player = _playerState.GetPlayerById(arg1);
            if (player.HasValue)
            {
                var nickname = player.Value.GetState().NickName;
                _coopStatusWidget.Value.RemovePlayer(nickname);
                RefreshWidgets();
            }
        }

        private void OnJoinedArea(AreaId area, Entity areaEntity)
        {
            _widgetManager.OnJoinedArea(area, areaEntity);
            var playerEntity = _playerState.LocalPlayerEntity;
            if (playerEntity.HasValue)
            {
                _coopStatusWidget.Value.AddPlayer(playerEntity.Value.GetState().NickName);
                RefreshWidgets();
            }
        }

        private void OnLeftArea(AreaId arg1, Entity arg2)
        {
            _widgetManager.OnLeftArea(arg1, arg2);
            var playerEntity = _playerState.LocalPlayerEntity;
            if (playerEntity.HasValue)
            {
                _coopStatusWidget.Value.RemovePlayer(playerEntity.Value.GetState().NickName);
                RefreshWidgets();
            }
        }
    }
}
