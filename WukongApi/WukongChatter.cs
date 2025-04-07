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

        private const string ServerPrefix = "<S>";
        private const string ClientPrefix = "<C>";
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
            if (!string.IsNullOrEmpty(message))
            {
                if (!TryHandleCommand(message))
                {
                    SendChatMessage(message);
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
        }

        private void RequestSpawn(ReadOnlyMemory<string> args)
        {
            switch (args.Length)
            {
                case 1:
                    GameLoopPatch.QueueOnGameThread(() => WukongMP.Instance.SpawnEnemiesMaster(args.Span[0], 1, _wukongClient.LocalPlayerState.TeamId), "SpawnEnemiesMaster");
                    SendServerMessage("Spawned monster");
                    break;
                case 2:
                {
                    if (int.TryParse(args.Span[1], out var count))
                    {
                        GameLoopPatch.QueueOnGameThread(() => WukongMP.Instance.SpawnEnemiesMaster(args.Span[0], count, _wukongClient.LocalPlayerState.TeamId), "SpawnEnemiesMaster");
                        SendServerMessage($"Spawned {count} monsters");
                    }

                    break;
                }
            }
        }

        private void RequestRebirth(ReadOnlyMemory<string> _)
        {
            GameLoopPatch.QueueOnGameThread(() => _wukongClient.BroadcastPlayerRebirth(_wukongClient.LocalPlayerState.PhotonId), "HandleRebirth");
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
            SendServerMessage($"{NickName} has left!");
            _wukongClient.StopRelayClient();
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

        private void SendChatMessage(string message)
        {
            Logging.LogDebug("Sending message {Message}", message);
            _wukongClient.SendChatMessage($"{ClientPrefix}{message}");
        }

        public void SendServerMessage(string message)
        {
            Logging.LogDebug("Sending server message {Message}", message);
            _wukongClient.SendChatMessage($"{ServerPrefix}{message}");
        }

        public void OnConnected()
        {
            Logging.LogDebug("Chat connected");
            SendServerMessage($"{NickName} has joined!");
        }

        public void OnGetMessage(int sender, string content)
        {
            var isServer = content.AsSpan()[..3] is ServerPrefix;
            var message = content[3..];
            var senderNickname = isServer ? "Server" : _wukongClient.GetById(sender)!.NickName;

            Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message, senderNickname);
            ChatWidget.Instance.AddMessage(isServer, senderNickname, message);
        }
    }
}