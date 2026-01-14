using b1;
using b1.BGW;
using BtlShare;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using System;
using System.Collections.Generic;
using System.Linq;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Chat;

public class WukongChatter : IDisposable
{
    private readonly WukongConnectionManager _connection;
    private readonly ClientState _state;
    private readonly WukongAreaState _areaState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongServerRpcCallbacks _serverRpc;
    private readonly WukongWidgetManager _widgetManager;
    private readonly WukongEventBus _eventBus;
    private readonly IClientEcsUpdateLoop _ecsLoop;

    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";
    private const char Separator = ' ';
    private readonly Dictionary<string, WukongChatterCommand> _commands = new();

    public WukongChatter(
        WukongConnectionManager connection,
        ClientState state,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc,
        WukongServerRpcCallbacks serverRpc,
        WukongWidgetManager widgetManager,
        WukongEventBus eventBus,
        IClientEcsUpdateLoop ecsLoop
    )
    {
        Logging.LogDebug("Initializing WukongChatter");

        _connection = connection;
        _state = state;
        _areaState = areaState;
        _playerState = playerState;
        _rpc = rpc;
        _serverRpc = serverRpc;
        _widgetManager = widgetManager;
        _eventBus = eventBus;
        _ecsLoop = ecsLoop;

        _state.OnJoinedArea += OnJoinedAreaHandler;
        _state.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideAreaHandler;

        _eventBus.OnLoadingScreenClose += OnLoadingScreenClose;

        _rpc.OnGetChatMessage += OnGetMessage;

        SetupCommands();
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing WukongChatter");

        _state.OnJoinedArea -= OnJoinedAreaHandler;
        _state.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideAreaHandler;

        _eventBus.OnLoadingScreenClose -= OnLoadingScreenClose;

        _rpc.OnGetChatMessage -= OnGetMessage;
    }

    public void ProcessMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = message.Trim();
            if (!TryHandleCommand(message))
            {
                SendChatMessage(NickName, message);
            }
        }
    }

    public void AddCommand(string command, WukongChatterCommand handler)
    {
        if (!_commands.ContainsKey(command))
        {
            _commands.Add(command, handler);
        }
    }

    private void SetupCommands()
    {
        AddCommand("/reconnect", new WukongChatterCommand(RequestReconnect));
        AddCommand("/giveup", new WukongChatterCommand(RequestGiveUp));
        AddCommand("/rebirth", new WukongChatterCommand(RequestRebirth));
        AddCommand("/rebirth_shrine", new WukongChatterCommand(RequestPointRebirth));
#if DEBUG
        AddCommand("/cheats", new WukongChatterCommand(ToggleCheats));
        AddCommand("/softlock", new WukongChatterCommand(ResolveSoftlock));
        AddCommand("/disconnect", new WukongChatterCommand(RequestDisconnect));
        AddCommand("/command", new WukongChatterCommand(ExecuteConsoleCommand));
        AddCommand("/colliders", new WukongChatterCommand(ToggleDynamicObstacles));
#endif
    }

    private void RequestRebirth(ReadOnlyMemory<string> _)
    {
        var playerId = _connection.PlayerId;
        if (playerId == null)
            return;

        _rpc.SendRebirthPlayer(playerId.Value);
        SendServerMessage("PlayerRequestedRebirth", NickName);
    }

    private void RequestPointRebirth(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        var playerId = mainEntity.GetState().PlayerId;
        PlayerUtils.TeleportLocalPlayerToRebirthPoint(mainEntity);
        _rpc.SendRebirthPlayer(playerId);
        SendServerMessage("PlayerRequestedRebirth", NickName);
    }

    private void ToggleCheats(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.IsMasterClient && _areaState.CurrentArea.HasValue)
        {
            var roomComp = _areaState.CurrentArea.Value.Room;
            // TODO: Move to server rpc response.
            SendServerMessage(roomComp.CheatsAllowed ? "CheatsDisabled" : "CheatsEnabled");
            _serverRpc.SendEnableCheats(_areaState.CurrentArea.Value.Scope.AreaId, !roomComp.CheatsAllowed);
        }
    }

    private void ResolveSoftlock(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        PlayerUtils.RespawnSoftlockedParty(mainEntity);
    }

    private void RequestGiveUp(ReadOnlyMemory<string> _)
    {
        SendServerMessage("PlayerGaveUp", NickName);

        // no need to send an RPC event since in co-op all players are authoritative over their HP
        _ecsLoop.Scheduler.Schedule(static (_, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            DebugUtils.InvincibilityEnabled = false; // otherwise we get black screen
            
            ref var localMainComp = ref mainEntity.GetLocalState();
            var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            events?.Evt_UnitDead.Invoke(localMainComp.Pawn, EDeadReason.Suicide);
        }, this);
    }

    private void RequestReconnect(ReadOnlyMemory<string> _)
    {
        _connection.Reconnect();
    }

    private void RequestDisconnect(ReadOnlyMemory<string> _)
    {
        if (_connection.AreaState.InRoom)
        {
            SendServerMessage("PlayerLeft", NickName);
            _connection.Disconnect();
        }
    }

    private void ExecuteConsoleCommand(ReadOnlyMemory<string> args)
    {
        var command = string.Join(" ", args.ToArray());
        Logging.LogDebug("Executing command: {Command}", command);
        USystemLibrary.ExecuteConsoleCommand(GameUtils.GetWorld(), command, null);
    }

    private void ToggleDynamicObstacles(ReadOnlyMemory<string> _)
    {
        try
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                UClass dynamicObstacleClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>("Blueprint'/Game/00Main/BPLibrary/SceneObj/BP_DynamicObstcle.BP_DynamicObstcle_C'", ELoadResourceType.SyncLoadAndCache);
                DebugUtils.ToggleBoxTemp(dynamicObstacleClass, world);
            }
        }
        catch (Exception e)
        {
            USharpExceptionHandler.HandleException(e, EUSharpExceptionType.NativeReflectionInvokeFunction);
        }
    }

    private bool TryHandleCommand(string message)
    {
        var commandParts = message.Split(Separator);
        if (commandParts.Length > 0)
        {
            if (_commands.ContainsKey(commandParts[0]))
            {
                if (CanExecuteCommand())
                {
                    var cmd = _commands[commandParts[0]];
                    var rest = commandParts.Skip(1).ToArray();
                    cmd.Handler(rest);
                }
                return true;
            }
        }

        return false;
    }

    private bool CanExecuteCommand()
    {
        return _playerState.LocalMainCharacter.HasValue && !_playerState.LocalMainCharacter.Value.GetLocalState().IsInSequence;
    }

    private void SendChatMessage(string nickname, string message)
    {
        Logging.LogDebug("Sending message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateClientMessage(nickname, message));
    }

    public void SendServerMessage(string message, params string[] args)
    {
        Logging.LogDebug("Sending server message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateServerMessage(message, args));
    }

    public void OnGetMessage(ChatMessage message)
    {
        var senderNickname = message.IsServer ? "Server" : message.Nickname!;
        var translatedMessage = message.Message;
        if (message.IsServer)
        {
            translatedMessage = string.Format(Texts.ResourceManager.GetString(message.Message, Texts.Culture)!, [.. message.Placeholders]);
        }

        Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message, senderNickname);
        _widgetManager.AddChatMessage(message.IsServer, senderNickname, translatedMessage);
    }

    public void AddLocalServerMessage(string message, params string[] placeholders)
    {
        var translatedMessage = string.Format(Texts.ResourceManager.GetString(message, Texts.Culture)!, [.. placeholders]);
        _widgetManager.AddChatMessage(true, "Server", translatedMessage);
    }

    public void AddLocalCommandMessage(string message)
    {
        _widgetManager.AddChatMessage(true, "Command", message);
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        Logging.LogDebug("Player {PlayerName} joined the room", player.NickName);
        SendServerMessage("PlayerJoined", player.NickName);


    }

    private void OnOtherPlayerOutsideAreaHandler(PlayerId arg1, AreaId arg2, ReadyM.Api.Multiplayer.Common.OtherPlayerOutsideAreaReason arg3)
    {
        var playerEntity = _playerState.GetPlayerById(arg1);
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        var nickname = player.NickName;
        AddLocalServerMessage("PlayerLeft", [nickname]);
    }

    private void OnLoadingScreenClose()
    {
        if (_eventBus.IsGameplayLevel && _areaState.CurrentArea.HasValue && _areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            AddLocalServerMessage("CheatsEnabled");
            return;
        }
    }

}