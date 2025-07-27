using System;
using System.Collections.Generic;
using System.Linq;
using ReadyM.Api.ECS.Idents;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Old.State;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old;

public class WukongChatter : IDisposable
{
    private readonly WukongConnectionManager _connection;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongSynchronizer _synchronizer;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongGameplaySettings _gameplaySettings;

    private string NickName => _playerRegistry.LocalPlayerState.NickName;
    private const char Separator = ' ';
    private readonly Dictionary<string, WukongChatterCommand> _commands = new();

    public WukongChatter(
        WukongConnectionManager connection,
        WukongPlayerRegistry playerRegistry,
        WukongSynchronizer synchronizer,
        WukongRpcCallbacks rpc,
        WukongGameplaySettings gameplaySettings
    )
    {
        Logging.LogDebug("Initializing WukongChatter");
        
        _connection = connection;
        _playerRegistry = playerRegistry;
        _synchronizer = synchronizer;
        _rpc = rpc;
        _gameplaySettings = gameplaySettings;

        _connection.OnMasterClientChanged += OnMasterClientChanged;
        _synchronizer.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
        _synchronizer.OnOtherPlayerLeft += OnOtherPlayerLeftHandler;
        
        SetupCommands();
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing WukongChatter");
        
        _synchronizer.OnOtherPlayerLeft -= OnOtherPlayerLeftHandler;
        _synchronizer.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
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
        _commands.Add("/hp_scaling", new WukongChatterCommand(SetMonsterHpScaling));
    }

    private void RequestSpawn(ReadOnlyMemory<string> args)
    {
        if (!UnitPathsConfig.IsValidMonsterName(args.Span[0]))
        {
            ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidUnitName}: \"{args.Span[0]}\"");
            return;
        }

        var teamId = PvPUtils.GetOppositeTeam(_playerRegistry.LocalPlayerState.TeamId);

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
        _rpc.SendRebirthPlayer(_connection.RelayClient.PlayerId);
        SendServerMessage("PlayerRequestedRebirth", NickName);
    }

    private void RequestPointRebirth(ReadOnlyMemory<string> _)
    {
        
        PlayerUtils.TeleportLocalPlayerToRebirthPoint();
        _rpc.SendRebirthPlayer(_connection.RelayClient.PlayerId);
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
        if (_connection.RoomState.InRoom)
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
            var username = args.Span[0];
            var isSpectator = args.Span[1].Equals("true", StringComparison.OrdinalIgnoreCase);

            var player = _playerRegistry.AllConnectedPlayers.FirstOrDefault(x => x.NickName == username);
            if (player == null)
                return;

            _playerProperty.SetRemotePlayerProperty(player.PlayerId, nameof(PlayerState.IsSpectator), isSpectator);
        }
    }

    private void SetMonsterHpScaling(ReadOnlyMemory<string> args)
    {
        if (args.Length == 1)
        {
            var hpScaling = args.Span[0];

            if (int.TryParse(hpScaling, out var scaling))
            {
                _gameplaySettings.SetMonsterHpScaling(scaling);
                SendServerMessage(nameof(Texts.SetMonsterHpScaling), scaling.ToString());
            }
            else
            {
                ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidCommand}: \"{hpScaling}\"");
            }
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
    
    private void OnAfterJoinedRoomHandler()
    {
        Logging.LogDebug("Player {PlayerName} joined the room", _playerRegistry.LocalPlayerState.NickName);
        SendServerMessage("PlayerJoined", _playerRegistry.LocalPlayerState.NickName);
    }

    private void OnOtherPlayerLeftHandler(PlayerId playerId)
    {
        if (_connection.RoomState.IsMasterClient)
        {
            var player = _connection.RelayClient.GetPlayerState(playerId)!;
            var nickname = (string)player.Properties.GetValueOrDefault(nameof(PlayerState.NickName), "Player");
            SendServerMessage("PlayerLeft", nickname);
        }
    }
}
