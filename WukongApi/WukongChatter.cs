using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Photon.Chat;
using Photon.Client;
using WukongApi.State;
using AuthenticationValues = Photon.Chat.AuthenticationValues;

namespace WukongApi
{
    internal class Command
    {
        public string Name { get; set; }
        public Action<string[]> Handler { get; set; }
    }

    public class WukongChatter : IChatClientListener
    {
        private ChatClient _chatClient;
        private readonly WukongClient _wukongClient;

        private const string ServerPrefix = "<S>";
        private const string ClientPrefix = "<C>";
        private string RoomName => _wukongClient.PhotonClient.CurrentRoom?.Name ?? Guid.NewGuid().ToString(); // do not collide with anybody if sth goes wrong
        private string GeneralChannelName => $"chat-${RoomName}";
        private string NickName => _wukongClient.LocalPlayerState.NickName;

        private bool _isExit;

        public event Func<string> OnGetMessage;
        public event Action<bool, string, string> OnSendMessage;

        public event Action OnSavePosition;
        public event Action OnLoadPosition;
        public event Action OnReconnectRequest;
        public event Action OnDisconnectRequest;
        public event Action OnRebirthRequested;
        public event Action<string, int, int> OnSpawnEnemy;

        private const char Separator = ' ';
        private readonly Dictionary<string, Command> _commands = new();

        public WukongChatter(WukongClient owner)
        {
            _wukongClient = owner;
            SetupCommands();

            new Thread(LoopChat).Start();
        }

        public void InitializeChat(string userId)
        {
            var authValues = new AuthenticationValues(userId)
            {
                AuthType = CustomAuthenticationType.Custom,
            };
            authValues.AddAuthParameter("access_token", CmdLineParams.Instance.AccessToken);

            _chatClient = new ChatClient(this)
            {
                AuthValues = authValues
            };

            _chatClient.ConnectUsingSettings(new ChatAppSettings
            {
                AppIdChat = Constants.ChatAppId,
                AppVersion = "1.0",
                FixedRegion = "us",
            });
        }

        public void Disconnect()
        {
            _chatClient?.Disconnect();
            StopMessageService();
        }

        private void StopMessageService()
        {
            _isExit = true;
        }

        private void SetupCommands()
        {
            _commands.Add(
                "/savePos",
                new Command
                {
                    Name = "Save checkpoint",
                    Handler = _ => { OnSavePosition?.Invoke(); }
                });

            _commands.Add(
                "/loadPos",
                new Command
                {
                    Name = "Load checkpoint",
                    Handler = _ => { OnLoadPosition?.Invoke(); }
                });

            _commands.Add(
                "/spawn",
                new Command
                {
                    Name = "Spawn enemy NPC",
                    Handler = args =>
                    {
                        switch (args.Length)
                        {
                            case 1:
                                OnSpawnEnemy?.Invoke(args[0], 1, _wukongClient.LocalPlayerState.TeamId);
                                SendServerMessage("Spawned monster");
                                break;
                            case 2:
                            {
                                if (int.TryParse(args[1], out var count))
                                {
                                    OnSpawnEnemy?.Invoke(args[0], count, _wukongClient.LocalPlayerState.TeamId);
                                    SendServerMessage($"Spawned {count} monsters");
                                }

                                break;
                            }
                        }
                    }
                });
            _commands.Add(
                "/reconnect",
                new Command
                {
                    Name = "Reconnect",
                    Handler = _ => { RequestReconnect(); }
                });
            _commands.Add(
                "/disconnect",
                new Command
                {
                    Name = "Disconnect",
                    Handler = _ => { RequestDisconnect(); }
                });
            _commands.Add(
                "/rebirth",
                new Command
                {
                    Name = "Rebirth",
                    Handler = _ => { RequestRebirth(); }
                });
            _commands.Add(
                "/giveup",
                new Command
                {
                    Name = "GiveUp",
                    Handler = _ => { RequestGiveUp(); }
                });
            _commands.Add(
                "/ready",
                new Command
                {
                    Name = "Ready",
                    Handler = _ => { _wukongClient.SetReadyState(true); }
                });
            _commands.Add(
                "/start",
                new Command
                {
                    Name = "Start",
                    Handler = _ => { _wukongClient.RequestStartPvP(); }
                });
        }

        private void RequestRebirth()
        {
            OnRebirthRequested?.Invoke();
            SendServerMessage($"Player {NickName} requested rebirth");
        }

        private void RequestGiveUp()
        {
            SendServerMessage($"Player {NickName} gave up");
            _wukongClient.KillCurrentPlayer();
        }

        public void RequestReconnect()
        {
            OnReconnectRequest?.Invoke();
        }

        private void RequestDisconnect()
        {
            SendServerMessage($"{NickName} has left!");
            OnDisconnectRequest?.Invoke();
        }

        private void ServiceChat()
        {
            _chatClient?.Service();

            var message = OnGetMessage?.Invoke();
            if (!string.IsNullOrEmpty(message))
            {
                if (!TryHandleCommand(message))
                {
                    if (_chatClient != null)
                    {
                        SendChatMessage(message);
                    }
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

        private void LoopChat()
        {
            while (!_isExit)
            {
                ServiceChat();
                Thread.Sleep(33);
            }
        }

        private void SendChatMessage(string message)
        {
            Logging.LogDebug("Sending message {Message}", message);
            _chatClient.PublishMessage(GeneralChannelName, $"{ClientPrefix}{message}");
        }

        public void SendServerMessage(string message)
        {
            Logging.LogDebug("Sending server message {Message}", message);
            _chatClient.PublishMessage(GeneralChannelName, $"{ServerPrefix}{message}");
        }

        public void DebugReturn(LogLevel level, string message) { }

        public void OnChatStateChange(ChatState state)
        {
            Logging.LogDebug("Chat state changed to: {State}", state);
        }

        public void OnConnected()
        {
            Logging.LogDebug("Chat connected");
            _chatClient.Subscribe(GeneralChannelName);
            SendServerMessage($"{NickName} has joined!");
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Logging.LogError("Chat authentication failed: {Message}", debugMessage);
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }

        public void OnDisconnected()
        {
            Logging.LogDebug("Chat disconnected");
        }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            for (var i = 0; i < senders.Length; i++)
            {
                var content = messages[i].ToString();
                var isServer = content.AsSpan()[..3] is ServerPrefix;
                var message = content[3..];

                Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message, senders[i]);
                OnSendMessage?.Invoke(isServer, isServer ? "Server" : senders[i], message);
            }
        }

        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            Logging.LogDebug("Private message \"{Message}\" received from \"{Sender}\" on channel \"{Channel}\"", message, sender, channelName);
        }

        public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }

        public void OnSubscribed(string[] channels, bool[] results)
        {
            for (var i = 0; i < channels.Length; i++)
            {
                Logging.LogDebug("Subscribed to the channel: {Channel}: {Result}", channels[i], results[i]);
            }
        }

        public void OnUnsubscribed(string[] channels) { }

        public void OnUserSubscribed(string channel, string user) { }

        public void OnUserUnsubscribed(string channel, string user) { }
    }
}