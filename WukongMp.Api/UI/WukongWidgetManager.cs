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

internal sealed class WukongWidgetManager : IDisposable
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

    public bool IsDebugViewVisible => _debugViewWidget.Value.IsVisible();

    public WukongWidgetManager(ClientState clientState, WukongPlayerState playerState, IRelayClient relayClient, WukongEventBus eventBus, FreeCameraManager freeCameraManager)
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

    public void AddSystemChatMessage(string message, FLinearColor color) => _chatWidget.Value.AddMessageWithColor(false, "", message, color);
    public void AddChatMessage(string sender, string message, FLinearColor color) => _chatWidget.Value.AddMessageWithColor(true, sender, message, color);

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

    public void SetSdkVersion(string version)
    {
        _fullModVersion = version;
        var subVersions = version.Split('+');
        if (subVersions.Length > 0)
            _shortModVersion = subVersions[0];
    }

    public void AddCharacterToDebugView(string name)
    {
        _debugViewWidget.Value.AddPlayer(name);
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

    public void SetTimerVisibility(bool visible)
    {
        _timerWidget.Value.SetVisibility(visible);
    }

    public void ToggleDebugVisibility() => _debugViewWidget.Value.ToggleVisibility();

    public void ToggleChatVisibility() => _chatWidget.Value.ToggleVisibility();

    public bool ChatHasFocus => _chatWidget.Value.HasFocus();

    public void SetChatInputFocus() => _chatWidget.Value.SetInputFocus();

    public string CommitChatMessage() => _chatWidget.Value.CommitMessage();

    public bool CommandHasFocus() => _commandConsoleWidget.Value.HasFocus();

    public bool IsCommandVisible() => _commandConsoleWidget.Value.IsVisible();

    public void CommandSelectUp() => _commandConsoleWidget.Value.SelectUp();

    public void CommandSelectDown() => _commandConsoleWidget.Value.SelectDown();

    public void CommandHistoryUp() => _commandConsoleWidget.Value.SetHistoryNext();

    public void CommandHistoryDown() => _commandConsoleWidget.Value.SetHistoryPrev();

    public void CommandSelectSuggestion() => _commandConsoleWidget.Value.SelectSuggestion();

    public void SetCommandInputFocus() => _commandConsoleWidget.Value.SetInputFocus();

    public string CommitCommand() => _commandConsoleWidget.Value.CommitCommand();


    public void SetSpectatingMessage(string message)
    {
        _freeCameraMessageWidget.Value.SetVisibility(true);
        _freeCameraMessageWidget.Value.SetMessageText(message);
    }

    public void HideSpectatingMessage() => _freeCameraMessageWidget.Value.SetVisibility(false);
}