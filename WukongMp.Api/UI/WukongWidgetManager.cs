using System;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI;

public sealed class WukongWidgetManager(ClientState clientState) : IDisposable
{
    private string _lastDisconnectText = Texts.Disconnected;

    private bool _isInitialized;

    private readonly Lazy<ChatWidget> _chatWidget = new();
    private readonly Lazy<InfoMessageWidget> _infoMessageWidget = new();
    private readonly Lazy<ErrorMessageWidget> _errorMessageWidget = new();
    private readonly Lazy<PingIndicatorWidget> _pingIndicatorWidget = new();
    private readonly Lazy<FreeCameraControlsWidget> _freeCameraControlsWidget = new();

    public void Dispose() { }

    public void OnFreeCameraModeChanged(bool enabled)
    {
        _freeCameraControlsWidget.Value.SetVisibility(enabled);
    }

    public void ShowInGameWidgets()
    {
        _pingIndicatorWidget.Value.SetVisibility(true);
        _chatWidget.Value.ShowIfNotHidden();
    }

    public void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
        InitializeWidgets();
        _chatWidget.Value.SetVisibility(false);

        if (!clientState.IsConnected)
        {
            DI.Instance.RelayClient.Scheduler.Schedule(static (ctx, self) =>
            {
                self._infoMessageWidget.Value.SetVisibility(true);
                self._lastDisconnectText = ctx.LastDisconnectReason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected;
                self._infoMessageWidget.Value.SetText(self._lastDisconnectText);
            }, this);
        }
    }

    public void UpdatePingIndicator(long pingMs)
    {
        _pingIndicatorWidget.Value.SetPingValue(pingMs);
        _pingIndicatorWidget.Value.HideInfoText();
    }

    public void SetPacketLossWarning()
    {
        _pingIndicatorWidget.Value.SetPingValue(999);
        _pingIndicatorWidget.Value.SetInfoText(Texts.SeverePacketLossDetected);
    }

    public void HideInfoMessage()
    {
        _infoMessageWidget.Value.SetVisibility(false);
    }

    public void ShowInfoMessage(string message)
    {
        _infoMessageWidget.Value.SetText(message);
        _infoMessageWidget.Value.SetVisibility(true);
    }

    public void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing widgets");
        DeinitializeWidgets();
    }

    public void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
    {
        _infoMessageWidget.Value.SetVisibility(true);
        _lastDisconnectText = reason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected;
        _infoMessageWidget.Value.SetText(_lastDisconnectText);
    }

    public void OnConnected(PlayerId playerId, Entity entity)
    {
        _infoMessageWidget.Value.SetVisibility(false);
    }

    private void InitializeWidgets()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            ModWidgetsUtils.SpawnWidgetManagerActor();

            _chatWidget.Value.Initialize();
            _infoMessageWidget.Value.Initialize();
            _errorMessageWidget.Value.Initialize();
            _pingIndicatorWidget.Value.Initialize();
            _freeCameraControlsWidget.Value.Initialize();
        }
    }

    private void DeinitializeWidgets()
    {
        _chatWidget.Value.Deinitialize();
        _infoMessageWidget.Value.Deinitialize();
        _errorMessageWidget.Value.Deinitialize();
        _pingIndicatorWidget.Value.Deinitialize();
        _freeCameraControlsWidget.Value.Deinitialize();
        _isInitialized = false;
    }

    public void ToggleChatVisibility() => _chatWidget.Value.ToggleVisibility();
    
    public void AddChatMessage(bool isSystemMessage, string sender, string message) => _chatWidget.Value.AddMessage(isSystemMessage, sender, message);

    public bool ChatHasFocus() => _chatWidget.Value.HasFocus();

    public void SetChatHistoryNext() => _chatWidget.Value.SetHistoryNext();
    
    public void SetChatHistoryPrev() => _chatWidget.Value.SetHistoryPrev();

    public void SetChatInputFocus() => _chatWidget.Value.SetInputFocus();

    public string CommitChatMessage() => _chatWidget.Value.CommitMessage();
}