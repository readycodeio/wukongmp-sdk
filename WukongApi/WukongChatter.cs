using System;
using System.Collections.Generic;
using System.Linq;
using WukongApi.Patches;
using WukongApi.UI;

namespace WukongApi
{
    internal class Command(Action<ReadOnlyMemory<string>> handler)
    {
        public Action<ReadOnlyMemory<string>> Handler { get; } = handler;
    }

    public class WukongChatter
    {
        private readonly WukongClient _wukongClient;

        private string NickName => _wukongClient.LocalPlayerState.NickName;
        private const char Separator = ' ';
        private readonly Dictionary<string, Command> _commands = new();

        public WukongChatter(WukongClient owner)
        {
            _wukongClient = owner;
            SetupCommands();
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
            _commands.Add("/spawn", new Command(RequestSpawn));
            _commands.Add("/reconnect", new Command(RequestReconnect));
            _commands.Add("/disconnect", new Command(RequestDisconnect));
            _commands.Add("/rebirth", new Command(RequestRebirth));
            _commands.Add("/giveup", new Command(RequestGiveUp));
            _commands.Add("/master", new Command(RequestNewMasterClient));
        }

        private void RequestSpawn(ReadOnlyMemory<string> args)
        {
            switch (args.Length)
            {
                case 1:
                    GameLoopPatch.QueueOnGameThread(() => WukongMP.Instance.SpawnEnemiesLocal(args.Span[0], 1, _wukongClient.LocalPlayerState.TeamId), "SpawnEnemiesMaster");
                    break;
                case 2:
                {
                    if (int.TryParse(args.Span[1], out var count))
                    {
                        GameLoopPatch.QueueOnGameThread(() => WukongMP.Instance.SpawnEnemiesLocal(args.Span[0], count, _wukongClient.LocalPlayerState.TeamId), "SpawnEnemiesMaster");
                    }
                    else
                    {
                        ChatWidget.Instance.AddMessage(true, "Command", $"Invalid number of enemies: \"{args.Span[1]}\"");
                    }

                    break;
                }
            }
        }

        private void RequestRebirth(ReadOnlyMemory<string> _)
        {
            GameLoopPatch.QueueOnGameThread(() => _wukongClient.BroadcastPlayerRebirth(_wukongClient.LocalPlayerState.PeerId), "HandleRebirth");
            SendServerMessage($"Player {NickName} requested rebirth");
        }

        private void RequestGiveUp(ReadOnlyMemory<string> _)
        {
            SendServerMessage($"Player {NickName} gave up");
            _wukongClient.KillCurrentPlayer();
        }

        private void RequestReconnect(ReadOnlyMemory<string> _)
        {
            _wukongClient.Reconnect();
        }

        private void RequestDisconnect(ReadOnlyMemory<string> _)
        {
            if (_wukongClient.ConnectedAndInRoom)
            {
                SendServerMessage($"{NickName} has left!");
                _wukongClient.StopRelayClient();
            }
        }

        private void RequestNewMasterClient(ReadOnlyMemory<string> args)
        {
            if (args.Length == 1)
            {
                _wukongClient.SetMasterClient(args.Span[0]);
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
            _wukongClient.SendChatMessage(ChatMessage.CreateClientMessage(nickname, message));
        }

        public void SendServerMessage(string message)
        {
            Logging.LogDebug("Sending server message {Message}", message);
            _wukongClient.SendChatMessage(ChatMessage.CreateServerMessage(message));
        }

        public void OnGetMessage(ChatMessage message)
        {
            var senderNickname = message.IsServer ? "Server" : message.Nickname!;
            Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message, senderNickname);
            ChatWidget.Instance.AddMessage(message.IsServer, senderNickname, message.Message);
        }
    }
}