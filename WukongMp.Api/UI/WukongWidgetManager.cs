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
    private readonly FreeCameraManager _freeCameraManager;

    private string _lastDisconnectText = Texts.Disconnected;

    private readonly ChatWidget _chatWidget = new();
    private readonly InfoMessageWidget _infoMessageWidget = new();
    private readonly ErrorMessageWidget _errorMessageWidget = new();
    private readonly PingIndicatorWidget _pingIndicatorWidget = new();
    private readonly FreeCameraControlsWidget _freeCameraControlsWidget = new();

    public WukongWidgetManager(ClientState pawnState, WukongPlayerState playerState, WukongEventBus eventBus, FreeCameraManager freeCameraManager)
    {
        _clientState = pawnState;
        _playerState = playerState;
        _eventBus = eventBus;
        _freeCameraManager = freeCameraManager;

        _clientState.OnConnected += OnConnected;
        _clientState.OnDisconnected += OnDisconnected;
        _eventBus.OnLevelLoaded += OnLevelLoaded;
        _eventBus.OnExitLevel += OnExitLevel;

        _freeCameraManager.OnFreeCameraModeChanged += OnFreeCameraModeChanged;
    }

    private void OnFreeCameraModeChanged(bool enabled)
    {
        _freeCameraControlsWidget.SetVisibility(enabled);
    }

    public void Dispose()
    {
        _clientState.OnConnected -= OnConnected;
        _clientState.OnDisconnected -= OnDisconnected;
        _eventBus.OnLevelLoaded -= OnLevelLoaded;
        _eventBus.OnExitLevel -= OnExitLevel;

        _freeCameraManager.OnFreeCameraModeChanged -= OnFreeCameraModeChanged;
    }

    public void ShowInGameWidgets()
    {
        _pingIndicatorWidget.SetVisibility(true);
        _chatWidget.ShowIfNotHidden();
    }

    public void AddChatMessage(bool isSystemMessage, string sender, string message)
    {
        _chatWidget.AddMessage(isSystemMessage, sender, message);
    }

    private void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
        ModWidgetsUtils.SpawnWidgetManagerActor(); // this needs to be shared
        InitializeWidgets();
        _chatWidget.SetVisibility(false);

        if (!_clientState.IsConnected)
        {
            DI.Instance.RelayClient.Scheduler.Schedule(ctx =>
            {
                _infoMessageWidget.SetVisibility(true);
                _lastDisconnectText = ctx.LastDisconnectReason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected;
                _infoMessageWidget.SetText(_lastDisconnectText);
            });
        }
    }

    public void UpdatePingIndicator(long pingMs)
    {
        _pingIndicatorWidget.SetPingValue(pingMs);
        _pingIndicatorWidget.HideInfoText();
    }

    public void SetPacketLossWarning()
    {
        _pingIndicatorWidget.SetPingValue(999);
        _pingIndicatorWidget.SetInfoText(Texts.SeverePacketLossDetected);
    }

    public void HideInfoMessage()
    {
        _infoMessageWidget.SetVisibility(false);
    }

    public void ShowInfoMessage(string message)
    {
        _infoMessageWidget.SetText(message);
        _infoMessageWidget.SetVisibility(true);
    }

    private void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing widgets");
        DeinitializeWidgets();
    }

    private void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
    {
        _infoMessageWidget.SetVisibility(true);
        _lastDisconnectText = reason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected;
        _infoMessageWidget.SetText(_lastDisconnectText);
    }

    private void OnConnected(PlayerId playerId, Entity entity)
    {
        _infoMessageWidget.SetVisibility(false);
    }

    public void InitializeWidgets()
    {
        _chatWidget.Initialize();
        _infoMessageWidget.Initialize();
        _errorMessageWidget.Initialize();
        _pingIndicatorWidget.Initialize();
        _freeCameraControlsWidget.Initialize();
    }

    public void DeinitializeWidgets()
    {
        _chatWidget.Deinitialize();
        _infoMessageWidget.Deinitialize();
        _errorMessageWidget.Deinitialize();
        _pingIndicatorWidget.Deinitialize();
        _freeCameraControlsWidget.Deinitialize();
    }
}