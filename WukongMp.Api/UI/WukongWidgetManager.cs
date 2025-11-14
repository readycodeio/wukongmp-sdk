using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using Sentry.Reflection;
using System;
using System.Reflection;
using UnrealEngine.Runtime;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI;

public sealed class WukongWidgetManager(ClientState clientState, WukongPlayerState playerState) : IDisposable
{
    private string _lastDisconnectText = Texts.Disconnected;

    private bool _isInitialized;

    private readonly Lazy<ChatWidget> _chatWidget = new();
    private readonly Lazy<InfoMessageWidget> _infoMessageWidget = new();
    private readonly Lazy<ErrorMessageWidget> _errorMessageWidget = new();
    private readonly Lazy<PingIndicatorWidget> _pingIndicatorWidget = new();
    private readonly Lazy<FreeCameraControlsWidget> _freeCameraControlsWidget = new();
    private readonly Lazy<ModVersionWidget> _modVersionWidget = new();
    private readonly Lazy<DebugViewWidget> _debugViewWidget = new();

    public void Dispose() { }

    public void OnFreeCameraModeChanged(bool enabled)
    {
        _freeCameraControlsWidget.Value.SetVisibility(enabled);
    }

    public void ShowInGameWidgets(bool isOnGameplayLevel)
    {
        if (isOnGameplayLevel)
        {
            _pingIndicatorWidget.Value.SetVisibility(true);
            _chatWidget.Value.ShowIfNotHidden();
        }
        _modVersionWidget.Value.SetVisibility(true);
    }

    public void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
        InitializeWidgets();
        _chatWidget.Value.SetVisibility(false);
        SetModVersionText(Assembly.GetExecutingAssembly().GetNameAndVersion().Version ?? "");

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

    public bool IsDebugViewVisible => _debugViewWidget.Value.IsVisible();

    public void SetModVersionText(string version)
    {
        _modVersionWidget.Value.SetVersionText(version);
        _debugViewWidget.Value.SetVersionText(version);
    }

    public void UpdatePlayerPosition(string playerName, FVector gameLocation, FVector ecsLocation)
    {
        _debugViewWidget.Value.SetPlayerPosition(playerName, gameLocation, ecsLocation);
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

    public void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
    {
        var player = playerState.GetPlayerById(playerId);
        if (player.HasValue)
        {
            _debugViewWidget.Value.AddPlayer(player.Value.GetState().NickName);
        }
    }

    public void OnOtherPlayerOutsideArea(PlayerId playerId, AreaId area, OtherPlayerOutsideAreaReason reason)
    {
        var player = playerState.GetPlayerById(playerId);
        if (player.HasValue)
        {
            _debugViewWidget.Value.RemovePlayer(player.Value.GetState().NickName);
        }
    }

    public void OnJoinedArea(AreaId area, Entity areaEntity)
    {
        var playerEntity = playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            _debugViewWidget.Value.AddPlayer(playerEntity.Value.GetState().NickName);
        }
    }

    public void OnLeftArea(AreaId arg1, Entity arg2)
    {
        var playerEntity = playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            _debugViewWidget.Value.RemovePlayer(playerEntity.Value.GetState().NickName);
        }
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
            _modVersionWidget.Value.Initialize();
            _debugViewWidget.Value.Initialize();
        }
    }

    private void DeinitializeWidgets()
    {
        _chatWidget.Value.Deinitialize();
        _infoMessageWidget.Value.Deinitialize();
        _errorMessageWidget.Value.Deinitialize();
        _pingIndicatorWidget.Value.Deinitialize();
        _freeCameraControlsWidget.Value.Deinitialize();
        _modVersionWidget.Value.Deinitialize();
        _debugViewWidget.Value.Deinitialize();
        _isInitialized = false;
    }

    public void ToggleDebugVisibility() => _debugViewWidget.Value.ToggleVisibility();

    public void ToggleChatVisibility() => _chatWidget.Value.ToggleVisibility();
    
    public void AddChatMessage(bool isSystemMessage, string sender, string message) => _chatWidget.Value.AddMessage(isSystemMessage, sender, message);

    public bool ChatHasFocus() => _chatWidget.Value.HasFocus();

    public void SetChatHistoryNext() => _chatWidget.Value.SetHistoryNext();
    
    public void SetChatHistoryPrev() => _chatWidget.Value.SetHistoryPrev();

    public void SetChatInputFocus() => _chatWidget.Value.SetInputFocus();

    public string CommitChatMessage() => _chatWidget.Value.CommitMessage();
}