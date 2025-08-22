using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Configuration;
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
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;

    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";
    private const char Separator = ' ';
    private readonly Dictionary<string, WukongChatterCommand> _commands = new();

    public WukongChatter(
        WukongConnectionManager connection,
        ClientState state,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc
    )
    {
        Logging.LogDebug("Initializing WukongChatter");
        
        _connection = connection;
        _state = state;
        _playerState = playerState;
        _rpc = rpc;

        _connection.OnMasterClientChanged += OnMasterClientChanged;
        _state.OnJoinedArea += OnJoinedAreaHandler;
        _state.OnLeftArea += OnLeftAreaHandler;
        
        SetupCommands();
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing WukongChatter");
        
        _state.OnLeftArea -= OnLeftAreaHandler;
        _state.OnJoinedArea -= OnJoinedAreaHandler;
        _connection.OnMasterClientChanged -= OnMasterClientChanged;
    }

    private void OnMasterClientChanged(string newMasterName)
    {
        SendServerMessage("MasterClient", newMasterName);
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

    private void SetupCommands()
    {
        _commands.Add("/spawn", new WukongChatterCommand(RequestSpawn));
        _commands.Add("/reconnect", new WukongChatterCommand(RequestReconnect));
        _commands.Add("/disconnect", new WukongChatterCommand(RequestDisconnect));
        _commands.Add("/rebirth", new WukongChatterCommand(RequestRebirth));
        _commands.Add("/rebirth_point", new WukongChatterCommand(RequestPointRebirth));
        _commands.Add("/giveup", new WukongChatterCommand(RequestGiveUp));
        _commands.Add("/master", new WukongChatterCommand(RequestNewMasterClient));
        _commands.Add("/spectator", new WukongChatterCommand(SetSpectatorStatus));
    }

    private void RequestSpawn(ReadOnlyMemory<string> args)
    {
        if (!UnitPathsConfig.IsValidMonsterName(args.Span[0]))
        {
            ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidUnitName}: \"{args.Span[0]}\"");
            return;
        }

        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        
        var teamId = PvPUtils.GetOppositeTeam(playerEntity.Value.GetState().TeamId);

        switch (args.Length)
        {
            case 1:
                _rpc.SendSpawnUnits(new UnitSpawnRequestData(args.Span[0], 1, teamId));
                break;
            case 2:
            {
                if (int.TryParse(args.Span[1], out var count))
                {
                    _rpc.SendSpawnUnits(new UnitSpawnRequestData(args.Span[0], count, teamId));
                }
                else
                {
                    ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidUnitName}: \"{args.Span[1]}\"");
                }

                break;
            }
        }
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

    private void RequestGiveUp(ReadOnlyMemory<string> _)
    {
        SendServerMessage("PlayerGaveUp", NickName);
        _rpc.SendSuicide();
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

    private void RequestNewMasterClient(ReadOnlyMemory<string> args)
    {
        if (args.Length == 1)
        {
            _connection.SetMasterClient(args.Span[0]);
        }
    }

    private void SetSpectatorStatus(ReadOnlyMemory<string> args)
    {
        if (args.Length == 2)
        {
            var isSpectator = args.Span[1].Equals("true", StringComparison.OrdinalIgnoreCase);

            var playerEntity = _playerState.LocalPlayerEntity;
            if (playerEntity == null)
                return;
            playerEntity.Value.GetState().IsSpectator = isSpectator;
        }
    }

    private bool TryHandleCommand(string message)
    {
        var commandParts = message.Split(Separator);
        if (commandParts.Length > 0)
        {
            if (_commands.ContainsKey(commandParts[0]))
            {
                var cmd = _commands[commandParts[0]];
                var rest = commandParts.Skip(1).ToArray();
                cmd.Handler(rest);
                return true;
            }
        }

        return false;
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

    public static void OnGetMessage(ChatMessage message)
    {
        var senderNickname = message.IsServer ? "Server" : message.Nickname!;
        var translatedMessage = message.Message;
        if (message.IsServer)
        {
            translatedMessage = string.Format(Texts.ResourceManager.GetString(message.Message, Texts.Culture)!, [.. message.Placeholders]);
        }

        Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message, senderNickname);
        ChatWidget.Instance.AddMessage(message.IsServer, senderNickname, translatedMessage);
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

    private void OnLeftAreaHandler(AreaId areaId, Entity entity)
    {
        if (_connection.AreaState.IsMasterClient)
        {
            var playerEntity = _playerState.LocalPlayerEntity;
            if (playerEntity == null)
                return;
            ref var player = ref playerEntity.Value.GetState();
            var nickname = player.NickName;
            SendServerMessage("PlayerLeft", nickname);
        }
    }
}
