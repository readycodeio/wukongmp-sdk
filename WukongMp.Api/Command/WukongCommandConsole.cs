using b1;
using b1.BGW;
using BtlShare;
using ReadyM.Relay.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Chat;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Command;

public class WukongCommandConsole : IDisposable
{
    private readonly WukongConnectionManager _connection;
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongServerRpcCallbacks _serverRpc;
    private readonly WukongChatter _wukongChatter;
    private readonly WukongWidgetManager _widgetManager;
    private readonly WukongEventBus _eventBus;
    private readonly WukongAreaState _areaState;
    private readonly IClientEcsUpdateLoop _ecsLoop;

    private readonly Dictionary<string, ConsoleCommand> _commands = new();
    private readonly Dictionary<string, IEnumerable<string>> _commandsParams = new();
    private const char Separator = ' ';
    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";

    public WukongCommandConsole(
        WukongConnectionManager connection,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc,
        WukongServerRpcCallbacks serverRpc,
        WukongChatter wukongChatter,
        WukongWidgetManager widgetManager,
        WukongEventBus eventBus,
        WukongAreaState areaState,
        IClientEcsUpdateLoop ecsLoop
    )
    {
        Logging.LogDebug("Initializing WukongCommandConsole");

        _connection = connection;
        _playerState = playerState;
        _rpc = rpc;
        _serverRpc = serverRpc;
        _wukongChatter = wukongChatter;
        _widgetManager = widgetManager;
        _eventBus = eventBus;
        _areaState = areaState;
        _ecsLoop = ecsLoop;

        _eventBus.OnLoadingScreenClose += OnLoadingScreenClose;

        SetupCommands();
    }

    public void Dispose()
    {
        _eventBus.OnLoadingScreenClose -= OnLoadingScreenClose;

        Logging.LogDebug("Disposing WukongCommandConsole");
    }

    public void ProcessCommand(string command)
    {
        if (!string.IsNullOrWhiteSpace(command))
        {
            command = command.Trim();
            TryHandleCommand(command);
        }
    }

    public void AddCommand(string command, ConsoleCommand handler, IEnumerable<string>? availableFirstParams = null)
    {
        if (!_commands.ContainsKey(command))
        {
            _commands.Add(command, handler);
            if (availableFirstParams != null)
            {
                _commandsParams[command] = availableFirstParams;
            }
            _widgetManager.UpdateConsoleCommands(GetAvailableCommands(), _commandsParams);
        }
    }

    public void AddMessageToConsole(string message)
    {
        _widgetManager.AddMessageToConsole(message);
    }

    public void AddLocalizedMessageToConsole(string message, params string[] placeholders)
    {
        var translatedMessage = string.Format(Texts.ResourceManager.GetString(message, Texts.Culture)!, [.. placeholders]);
        _widgetManager.AddMessageToConsole(translatedMessage);
    }

    private void SetupCommands()
    {
        AddCommand("/reconnect", new ConsoleCommand(RequestReconnect));
        AddCommand("/giveup", new ConsoleCommand(RequestGiveUp));
        AddCommand("/rebirth", new ConsoleCommand(RequestRebirth));
        AddCommand("/rebirth_shrine", new ConsoleCommand(RequestPointRebirth));
#if DEBUG
        AddCommand("/cheats", new ConsoleCommand(ToggleCheats));
        AddCommand("/softlock", new ConsoleCommand(ResolveSoftlock));
        AddCommand("/disconnect", new ConsoleCommand(RequestDisconnect));
        AddCommand("/command", new ConsoleCommand(ExecuteConsoleCommand));
        AddCommand("/colliders", new ConsoleCommand(ToggleDynamicObstacles));
#endif
    }

    private void RequestRebirth(ReadOnlyMemory<string> _)
    {
        var playerId = _connection.PlayerId;
        if (playerId == null)
            return;

        _rpc.SendRebirthPlayer(playerId.Value);
        _wukongChatter.SendServerMessage("PlayerRequestedRebirth", NickName);
    }

    private void RequestPointRebirth(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        var playerId = mainEntity.GetState().PlayerId;
        PlayerUtils.TeleportLocalPlayerToRebirthPoint(mainEntity);
        _rpc.SendRebirthPlayer(playerId);
        _wukongChatter.SendServerMessage("PlayerRequestedRebirth", NickName);
    }

    private void ToggleCheats(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.IsMasterClient && _areaState.CurrentArea.HasValue)
        {
            var roomComp = _areaState.CurrentArea.Value.Room;
            // TODO: Move to server rpc response.
            _wukongChatter.SendServerMessage(roomComp.CheatsAllowed ? "CheatsDisabled" : "CheatsEnabled");
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
        _wukongChatter.SendServerMessage("PlayerGaveUp", NickName);

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
            _wukongChatter.SendServerMessage("PlayerLeft", NickName);
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
            else
            {
                AddLocalizedMessageToConsole("InvalidCommand", commandParts[0]);
            }
        }

        return false;
    }

    private bool CanExecuteCommand()
    {
        return _playerState.LocalMainCharacter.HasValue && !_playerState.LocalMainCharacter.Value.GetLocalState().IsInSequence;
    }

    private List<string> GetAvailableCommands()
    {
        return [.. _commands.Keys];
    }

    private void OnLoadingScreenClose()
    {
        if (_eventBus.IsGameplayLevel && _areaState.CurrentArea.HasValue && _areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            _wukongChatter.AddLocalServerMessage("CheatsEnabled");
            return;
        }
    }
}
