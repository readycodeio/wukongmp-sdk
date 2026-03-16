using System;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Relay.Client.State;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Resources;
using WukongMp.Api.State;

namespace WukongMp.Api.UI;

public sealed class WukongWidgetManager : IDisposable
{
    private string _lastDisconnectText = BuiltinTexts.Disconnected;

    private bool _isInitialized;

    private string _fullModVersion = "";
    private string _shortModVersion = "";

    // lazily initialized from DI, since otherwise we get a circular dependency
    private readonly Lazy<CommandConsoleWidget> _commandConsoleWidget = new(() => new CommandConsoleWidget(DI.Instance.CommandConsole));
    private readonly Lazy<ChatWidget> _chatWidget = new();
    private readonly Lazy<InfoMessageWidget> _infoMessageWidget = new();
    private readonly Lazy<ErrorMessageWidget> _errorMessageWidget = new();
    private readonly Lazy<PingIndicatorWidget> _pingIndicatorWidget = new();
    private readonly Lazy<FreeCameraControlsWidget> _freeCameraControlsWidget = new();
    private readonly Lazy<FreeCameraMessageWidget> _freeCameraMessageWidget = new();
    private readonly Lazy<ModVersionWidget> _modVersionWidget = new();
    private readonly Lazy<DebugViewWidget> _debugViewWidget = new();
    private readonly Lazy<TimerWidget> _timerWidget = new();
    private readonly ClientState clientState;
    private readonly WukongPlayerState playerState;
    private readonly IRelayClient relayClient;
    private readonly WukongEventBus eventBus;
    private readonly FreeCameraManager freeCameraManager;

    internal bool IsDebugViewVisible => _debugViewWidget.Value.IsVisible();

    internal WukongWidgetManager(ClientState clientState, WukongPlayerState playerState, IRelayClient relayClient, WukongEventBus eventBus, FreeCameraManager freeCameraManager)
    {
        this.clientState = clientState;
        this.playerState = playerState;
        this.relayClient = relayClient;
        this.eventBus = eventBus;
        this.freeCameraManager = freeCameraManager;

        clientState.OnConnected += OnConnected;
        clientState.OnDisconnected += OnDisconnected;
        clientState.OnJoinedArea += OnJoinedArea;
        clientState.OnLeftArea += OnLeftArea;
        clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;
        eventBus.OnLevelLoaded += OnLevelLoaded;
        eventBus.OnExitLevel += OnExitLevel;
        freeCameraManager.OnFreeCameraModeChanged += OnFreeCameraModeChanged;
    }

    public void Dispose()
    {
        clientState.OnConnected -= OnConnected;
        clientState.OnDisconnected -= OnDisconnected;
        clientState.OnJoinedArea -= OnJoinedArea;
        clientState.OnLeftArea -= OnLeftArea;
        clientState.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
        clientState.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;
        eventBus.OnLevelLoaded -= OnLevelLoaded;
        eventBus.OnExitLevel -= OnExitLevel;
        freeCameraManager.OnFreeCameraModeChanged -= OnFreeCameraModeChanged;

        DeinitializeWidgets();
    }

    #region Public API

    public void AddChatMessage(bool isSystemMessage, string sender, string message, FLinearColor color) => _chatWidget.Value.AddMessageWithColor(!isSystemMessage, sender, message, color);

    public void ToggleCommandVisibility() => _commandConsoleWidget.Value.ToggleVisibility();

    public void AddMessageToConsole(string message) => _commandConsoleWidget.Value.AddMessage("> " + message);

    public void ShowInGameWidgets(bool isOnGameplayLevel)
    {
        if (isOnGameplayLevel)
        {
            _pingIndicatorWidget.Value.SetVisibility(true);
            _chatWidget.Value.ShowIfNotHidden();
        }

        _modVersionWidget.Value.SetVisibility(true);
    }

    public void ShowInfoMessage(string message)
    {
        _infoMessageWidget.Value.SetText(message);
        _infoMessageWidget.Value.SetVisibility(true);
    }

    public void HideInfoMessage()
    {
        _infoMessageWidget.Value.SetVisibility(false);
    }

    #endregion

    internal void SetModVersion(string version)
    {
        _fullModVersion = version;
        var subVersions = version.Split('+');
        if (subVersions.Length > 0)
            _shortModVersion = subVersions[0];
    }

    internal void AddCharacterToDebugView(string name)
    {
        _debugViewWidget.Value.AddPlayer(name);
    }

    internal void UpdatePlayerPosition(string playerName, FVector gameLocation, FVector ecsLocation)
    {
        _debugViewWidget.Value.SetPlayerPosition(playerName, gameLocation, ecsLocation);
    }

    internal void UpdatePingIndicator(long pingMs)
    {
        _pingIndicatorWidget.Value.SetPingValue(pingMs);
        _pingIndicatorWidget.Value.HideInfoText();
    }

    internal void SetPacketLossWarning()
    {
        _pingIndicatorWidget.Value.SetPingValue(999);
        _pingIndicatorWidget.Value.SetInfoText(BuiltinTexts.SeverePacketLossDetected);
    }

    private void OnFreeCameraModeChanged(bool enabled)
    {
        _freeCameraControlsWidget.Value.SetVisibility(enabled);
    }

    private void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
        InitializeWidgets();
        _chatWidget.Value.SetVisibility(false);
        SetModVersionText();

        if (!clientState.IsConnected)
        {
            relayClient.Scheduler.Schedule(static (ctx, self) =>
            {
                self._infoMessageWidget.Value.SetVisibility(true);
                self._lastDisconnectText = ctx.LastDisconnectReason == DisconnectReason.ConnectionRejected ? BuiltinTexts.ConnectionRejectedByServer : BuiltinTexts.Disconnected;
                self._infoMessageWidget.Value.SetText(self._lastDisconnectText);
            }, this);
        }
    }

    private void SetModVersionText()
    {
        _modVersionWidget.Value.SetVersionText(_shortModVersion);
        _debugViewWidget.Value.SetVersionText(_fullModVersion);
    }

    private void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing widgets");
        DeinitializeWidgets();
    }

    private void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectReason reason)
    {
        _infoMessageWidget.Value.SetVisibility(true);
        _lastDisconnectText = reason == DisconnectReason.ConnectionRejected ? BuiltinTexts.ConnectionRejectedByServer : BuiltinTexts.Disconnected;
        _infoMessageWidget.Value.SetText(_lastDisconnectText);
    }

    private void OnConnected(PlayerId playerId, Entity entity)
    {
        _infoMessageWidget.Value.SetVisibility(false);
    }

    private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
    {
        var player = playerState.GetPlayerById(playerId);
        if (player.HasValue)
        {
            _debugViewWidget.Value.AddPlayer(player.Value.GetState().Nickname);
        }
    }

    private void OnOtherPlayerOutsideArea(PlayerId playerId, AreaId area, OtherPlayerOutsideAreaReason reason)
    {
        var player = playerState.GetPlayerById(playerId);
        if (player.HasValue)
        {
            _debugViewWidget.Value.RemovePlayer(player.Value.GetState().Nickname);
        }
    }

    private void OnJoinedArea(AreaId area, Entity areaEntity)
    {
        var playerEntity = playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            _debugViewWidget.Value.AddPlayer(playerEntity.Value.GetState().Nickname);
        }

        AreaEntity joinedAreaEntity = new(areaEntity);
        _chatWidget.Value.SetWritingEnabled(joinedAreaEntity.GetRoom().ChatEnabled);
    }

    private void OnLeftArea(AreaId arg1, Entity arg2)
    {
        var playerEntity = playerState.LocalPlayerEntity;
        if (playerEntity.HasValue)
        {
            _debugViewWidget.Value.RemovePlayer(playerEntity.Value.GetState().Nickname);
        }
    }

    private void InitializeWidgets()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;

            _commandConsoleWidget.Value.Initialize();
            _chatWidget.Value.Initialize();
            _infoMessageWidget.Value.Initialize();
            _errorMessageWidget.Value.Initialize();
            _pingIndicatorWidget.Value.Initialize();
            _freeCameraControlsWidget.Value.Initialize();
            _freeCameraMessageWidget.Value.Initialize();
            _modVersionWidget.Value.Initialize();
            _debugViewWidget.Value.Initialize();
            _timerWidget.Value.Initialize();
        }
    }

    private void DeinitializeWidgets()
    {
        _commandConsoleWidget.Value.Deinitialize();
        _chatWidget.Value.Deinitialize();
        _infoMessageWidget.Value.Deinitialize();
        _errorMessageWidget.Value.Deinitialize();
        _pingIndicatorWidget.Value.Deinitialize();
        _freeCameraControlsWidget.Value.Deinitialize();
        _freeCameraMessageWidget.Value.Deinitialize();
        _modVersionWidget.Value.Deinitialize();
        _debugViewWidget.Value.Deinitialize();
        _timerWidget.Value.Deinitialize();
        _isInitialized = false;
    }

    public void SetTimerText(int minutes, int seconds)
    {
        _timerWidget.Value.SetText(minutes, seconds);
    }

    internal void SetTimerVisibility(bool visible)
    {
        _timerWidget.Value.SetVisibility(visible);
    }

    internal void ToggleDebugVisibility() => _debugViewWidget.Value.ToggleVisibility();

    internal void ToggleChatVisibility() => _chatWidget.Value.ToggleVisibility();

    internal bool ChatHasFocus => _chatWidget.Value.HasFocus();

    internal void SetChatInputFocus() => _chatWidget.Value.SetInputFocus();

    internal string CommitChatMessage() => _chatWidget.Value.CommitMessage();

    internal bool CommandHasFocus() => _commandConsoleWidget.Value.HasFocus();

    internal bool IsCommandVisible() => _commandConsoleWidget.Value.IsVisible();

    internal void CommandSelectUp() => _commandConsoleWidget.Value.SelectUp();

    internal void CommandSelectDown() => _commandConsoleWidget.Value.SelectDown();

    internal void CommandHistoryUp() => _commandConsoleWidget.Value.SetHistoryNext();

    internal void CommandHistoryDown() => _commandConsoleWidget.Value.SetHistoryPrev();

    internal void CommandSelectSuggestion() => _commandConsoleWidget.Value.SelectSuggestion();

    internal void SetCommandInputFocus() => _commandConsoleWidget.Value.SetInputFocus();

    internal string CommitCommand() => _commandConsoleWidget.Value.CommitCommand();


    internal void SetSpectatingMessage(string message)
    {
        _freeCameraMessageWidget.Value.SetVisibility(true);
        _freeCameraMessageWidget.Value.SetMessageText(message);
    }

    internal void HideSpectatingMessage() => _freeCameraMessageWidget.Value.SetVisibility(false);
}