using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using System;
using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongMp.Api.Command;
using WukongMp.Api.Resources;
using WukongMp.Api.State;

namespace WukongMp.Api.UI;

public sealed class WukongWidgetManager(ClientState clientState, WukongPlayerState playerState) : IDisposable
{
    private string _lastDisconnectText = Texts.Disconnected;

    private bool _isInitialized;

    private string _fullModVersion = "";
    private string _shortModVersion = "";
    private List<string> _availableCommands = [];

    private readonly Lazy<CommandConsoleWidget> _commandConsoleWidget = new();
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

    public void SetModVersion(string version)
    {
        _fullModVersion = version;
        var subVersions = version.Split('+');
        if (subVersions.Length > 0)
            _shortModVersion = subVersions[0];
    }

    public void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing widgets");
        InitializeWidgets();
        _chatWidget.Value.SetVisibility(false);
        SetModVersionText();

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

    public void UpdateConsoleCommands(List<string> commands) => _availableCommands = commands;

    public bool IsDebugViewVisible => _debugViewWidget.Value.IsVisible();

    private void SetModVersionText()
    {
        _modVersionWidget.Value.SetVersionText(_shortModVersion);
        _debugViewWidget.Value.SetVersionText(_fullModVersion);
    }

    public void UpdatePlayerPosition(string playerName, FVector gameLocation, FVector ecsLocation)
    {
        _debugViewWidget.Value.SetPlayerPosition(playerName, gameLocation, ecsLocation);
    }

    public void AddCharacterToDebugView(string name)
    {
        _debugViewWidget.Value.AddPlayer(name);
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

            _commandConsoleWidget.Value.Initialize();
            _chatWidget.Value.Initialize();
            _infoMessageWidget.Value.Initialize();
            _errorMessageWidget.Value.Initialize();
            _pingIndicatorWidget.Value.Initialize();
            _freeCameraControlsWidget.Value.Initialize();
            _modVersionWidget.Value.Initialize();
            _debugViewWidget.Value.Initialize();

            _commandConsoleWidget.Value.SetAvailableCommands(_availableCommands);
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
        _modVersionWidget.Value.Deinitialize();
        _debugViewWidget.Value.Deinitialize();
        _isInitialized = false;
    }

    public void ToggleDebugVisibility() => _debugViewWidget.Value.ToggleVisibility();

    public void ToggleChatVisibility() => _chatWidget.Value.ToggleVisibility();

    public void AddChatMessage(bool isSystemMessage, string sender, string message) => _chatWidget.Value.AddMessage(isSystemMessage, sender, message);

    public bool ChatHasFocus() => _chatWidget.Value.HasFocus();

    public void SetChatInputFocus() => _chatWidget.Value.SetInputFocus();

    public string CommitChatMessage() => _chatWidget.Value.CommitMessage();

    public void ToggleCommandVisibility() => _commandConsoleWidget.Value.ToggleVisibility();

    public bool CommandHasFocus() => _commandConsoleWidget.Value.HasFocus();

    public bool IsCommandVisible() => _commandConsoleWidget.Value.IsVisible();

    public void CommandSelectUp() => _commandConsoleWidget.Value.SelectUp();

    public void CommandSelectDown() => _commandConsoleWidget.Value.SelectDown();

    public void CommandHistoryUp() => _commandConsoleWidget.Value.SetHistoryNext();

    public void CommandHistoryDown() => _commandConsoleWidget.Value.SetHistoryPrev();

    public void CommandSelectSuggestion() => _commandConsoleWidget.Value.SelectSuggestion();

    public void SetCommandInputFocus() => _commandConsoleWidget.Value.SetInputFocus();

    public string CommitCommand() => _commandConsoleWidget.Value.CommitCommand();

    public void AddMessageToConsole(string message) => _commandConsoleWidget.Value.AddMessage(message);
}