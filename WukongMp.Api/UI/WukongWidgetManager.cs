using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Client.State;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI;

internal sealed class WukongWidgetManager(
    ClientState clientState,
    WukongPlayerState playerState,
    IRelayClient relayClient,
    WukongEventBus eventBus,
    FreeCameraManager freeCameraManager,
    IChatSettings chatSettings
) : IHostedService
{
    private string _lastDisconnectText = BuiltinTexts.Disconnected;

    private bool _isInitialized;

    /// Names currently shown in the player list, mirroring what was pushed into the widget.
    private readonly HashSet<string> _listedPlayers = new(StringComparer.Ordinal);

    /// Scratch sets for the reconcile, kept as fields so a sync per area event allocates nothing.
    private readonly HashSet<string> _desiredPlayers = new(StringComparer.Ordinal);
    private readonly List<string> _stalePlayers = [];

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

    public bool IsDebugViewVisible => _debugViewWidget.Value.IsVisible();

    public void OnScopeStart()
    {
        clientState.OnConnected += OnConnected;
        clientState.OnDisconnected += OnDisconnected;
        clientState.OnJoinedArea += OnJoinedArea;
        clientState.OnLeftArea += OnLeftArea;
        clientState.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        clientState.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;
        // The inside-area event can beat the player entity, and so the nickname, so reconcile again
        // once the entity exists.
        clientState.OnOtherPlayerCreated += OnOtherPlayerCreated;
        clientState.OnOtherPlayerDeleted += OnOtherPlayerDeleted;
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
        clientState.OnOtherPlayerCreated -= OnOtherPlayerCreated;
        clientState.OnOtherPlayerDeleted -= OnOtherPlayerDeleted;
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

    public void UpdatePingIndicator(long pingMs, int packetLossPercent)
    {
        _pingIndicatorWidget.Value.SetPingValue(pingMs);

        if (packetLossPercent >= WukongMp.Api.Configuration.Constants.SeverePacketLossPercent)
        {
            _pingIndicatorWidget.Value.SetInfoText(BuiltinTexts.SeverePacketLossDetected);
        }
        else
        {
            _pingIndicatorWidget.Value.HideInfoText();
        }
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

        // The new widget starts empty, so drop what the old one was showing before reconciling.
        _listedPlayers.Clear();
        SyncPlayerList();

        // Re-checked inside the scheduled action rather than here. A transition long enough to trip
        // DisconnectTimeout reconnects about a second later, so testing connectivity before the
        // scheduler hop would show the banner after the reconnect had already hidden it, and nothing
        // would hide it again.
        relayClient.Scheduler.Schedule(static (ctx, self) => self.RestoreConnectionBanner(ctx.LastDisconnectedReason), this);
    }

    /// <summary>
    /// Reinstates the disconnect banner on a fresh set of widgets, but only while still disconnected.
    /// </summary>
    private void RestoreConnectionBanner(DisconnectedReason reason)
    {
        if (clientState.IsConnected)
        {
            _infoMessageWidget.Value.SetVisibility(false);
            return;
        }

        OnDisconnected(default, null, reason);
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

    private void OnDisconnected(PlayerId playerId, Entity? entity, DisconnectedReason reason)
    {
        HideOverlappingBanners();

        _infoMessageWidget.Value.SetVisibility(true);
        _lastDisconnectText = reason switch
        {
            DisconnectedReason.Unknown => BuiltinTexts.Disconnected,
            DisconnectedReason.Timeout => BuiltinTexts.Disconnected,
            DisconnectedReason.IncompatibleVersion => BuiltinTexts.IncompatibleVersion,
            DisconnectedReason.ExpiredTicket => BuiltinTexts.ConnectionRejectedByServer,
            DisconnectedReason.AlreadyConnected => BuiltinTexts.AlreadyConnected,
            DisconnectedReason.ClientDisconnected => BuiltinTexts.Disconnected,
            DisconnectedReason.ServerFull => BuiltinTexts.ServerFull,
            DisconnectedReason.Kicked => BuiltinTexts.Kicked,
            DisconnectedReason.Banned => BuiltinTexts.Banned,
            DisconnectedReason.ServerBanned => BuiltinTexts.ServerBanned,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
        _infoMessageWidget.Value.SetText(_lastDisconnectText);
    }

    /// The disconnect message occupies the same part of the screen as these.
    private void HideOverlappingBanners()
    {
        UiUtils.HideTip();
        _freeCameraMessageWidget.Value.SetVisibility(false);
        _timerWidget.Value.SetVisibility(false);
    }

    private void OnConnected(PlayerId playerId, Entity entity)
    {
        _infoMessageWidget.Value.SetVisibility(false);
    }

    private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area, OtherPlayerInsideAreaReason reason)
    {
        SyncPlayerList();
    }

    private void OnOtherPlayerCreated(PlayerId playerId, Entity entity, OtherPlayerCreatedReason reason)
    {
        SyncPlayerList();
    }

    private void OnOtherPlayerDeleted(PlayerId playerId, Entity entity, OtherPlayerDeletedReason reason)
    {
        SyncPlayerList();
    }

    private void OnOtherPlayerOutsideArea(PlayerId playerId, AreaId area, OtherPlayerOutsideAreaReason reason)
    {
        SyncPlayerList();
    }

    private void OnJoinedArea(AreaId area, Entity areaEntity)
    {
        SyncPlayerList();

        _chatWidget.Value.SetWritingEnabled(chatSettings.ChatEnabled);
        Logging.LogInformation("Chat enabled: {ChatEnabled}", chatSettings.ChatEnabled);
    }

    private void OnLeftArea(AreaId arg1, Entity arg2)
    {
        SyncPlayerList();
    }

    /// <summary>
    /// Reconciles the player list against the players actually in the area.
    /// </summary>
    /// <remarks>
    /// The widget only offers incremental add and remove, and drops calls while it is not ready, so
    /// the listed names are tracked here and diffed rather than assumed. Accumulating add and remove
    /// straight from the events lost a player for good whenever one arrived before their nickname had
    /// synced, or while a level change was swapping the widget out.
    /// </remarks>
    private void SyncPlayerList()
    {
        _desiredPlayers.Clear();
        foreach (var playerId in clientState.AreaPlayers)
        {
            var player = playerState.GetPlayerById(playerId);
            if (player.HasValue)
            {
                _desiredPlayers.Add(player.Value.GetState().Nickname.ToString());
            }
        }

        _stalePlayers.Clear();
        foreach (var listed in _listedPlayers)
        {
            if (!_desiredPlayers.Contains(listed))
            {
                _stalePlayers.Add(listed);
            }
        }

        foreach (var stale in _stalePlayers)
        {
            _debugViewWidget.Value.RemovePlayer(stale);
            _listedPlayers.Remove(stale);
        }

        foreach (var desired in _desiredPlayers)
        {
            if (_listedPlayers.Add(desired))
            {
                _debugViewWidget.Value.AddPlayer(desired);
            }
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