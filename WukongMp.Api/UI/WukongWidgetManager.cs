using System;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI;

public sealed class WukongWidgetManager : IDisposable
{
    private readonly ClientState _clientState;

    private string _lastDisconnectText = Texts.Disconnected;

    private bool _isInitialized;

    private readonly ChatWidget _chatWidget = new();
    private readonly InfoMessageWidget _infoMessageWidget = new();
    private readonly ErrorMessageWidget _errorMessageWidget = new();
    private readonly PingIndicatorWidget _pingIndicatorWidget = new();
    private readonly FreeCameraControlsWidget _freeCameraControlsWidget = new();

    public WukongWidgetManager(ClientState clientState)
    {
        _clientState = clientState;
    }

    public void Dispose() { }

    public void OnFreeCameraModeChanged(bool enabled)
    {
        _freeCameraControlsWidget.SetVisibility(enabled);
    }

    public void ShowInGameWidgets()
    {
        _pingIndicatorWidget.SetVisibility(true);
        _chatWidget.ShowIfNotHidden();
    }

    public void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
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

    public void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing widgets");
        DeinitializeWidgets();
    }

    public void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
    {
        _infoMessageWidget.SetVisibility(true);
        _lastDisconnectText = reason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected;
        _infoMessageWidget.SetText(_lastDisconnectText);
    }

    public void OnConnected(PlayerId playerId, Entity entity)
    {
        _infoMessageWidget.SetVisibility(false);
    }

    private void InitializeWidgets()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            ModWidgetsUtils.SpawnWidgetManagerActor();

            _chatWidget.Initialize();
            _infoMessageWidget.Initialize();
            _errorMessageWidget.Initialize();
            _pingIndicatorWidget.Initialize();
            _freeCameraControlsWidget.Initialize();
        }
    }

    private void DeinitializeWidgets()
    {
        _chatWidget.Deinitialize();
        _infoMessageWidget.Deinitialize();
        _errorMessageWidget.Deinitialize();
        _pingIndicatorWidget.Deinitialize();
        _freeCameraControlsWidget.Deinitialize();
        _isInitialized = false;
    }

    public void ToggleChatVisibility() => _chatWidget.ToggleVisibility();
    
    public void AddChatMessage(bool isSystemMessage, string sender, string message) => _chatWidget.AddMessage(isSystemMessage, sender, message);

    public bool ChatHasFocus() => _chatWidget.HasFocus();

    public void SetChatHistoryNext() => _chatWidget.SetHistoryNext();
    
    public void SetChatHistoryPrev() => _chatWidget.SetHistoryPrev();

    public void SetChatInputFocus() => _chatWidget.SetInputFocus();

    public string CommitChatMessage() => _chatWidget.CommitMessage();
}