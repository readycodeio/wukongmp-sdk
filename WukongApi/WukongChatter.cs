using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Photon.Chat;
using Photon.Client;
using AuthenticationValues = Photon.Chat.AuthenticationValues;

namespace WukongApi
{
    internal class Command(Action<string[]> handler)
    {
        public Action<string[]> Handler { get; set; } = handler;
    }

    public class WukongChatter : IChatClientListener
    {
        private ChatClient? _chatClient;
        private readonly WukongClient _wukongClient;

        private const string ServerPrefix = "<S>";
        private const string ClientPrefix = "<C>";
        private string RoomName => _wukongClient.PhotonClient.CurrentRoom?.Name ?? Guid.NewGuid().ToString(); // do not collide with anybody if sth goes wrong
        private string GeneralChannelName => $"chat-${RoomName}";
        private string NickName => _wukongClient.LocalPlayerState.NickName;

        private bool _isStopped = true;
        private Func<string>? _onGetMessage;

        public event Action<bool, string, string>? OnSendMessage;
        public event Action? OnSavePosition;
        public event Action? OnLoadPosition;
        public event Action? OnReconnectRequest;
        public event Action? OnDisconnectRequest;
        public event Action? OnRebirthRequested;
        public event Action<string, int, int>? OnSpawnEnemy;

        private const char Separator = ' ';
        private readonly Dictionary<string, Command> _commands = new();

        public WukongChatter(WukongClient owner)
        {
            _wukongClient = owner;
            SetupCommands();
        }

        public void SetMessageCallback(Func<string> getMessage)
        {
            _onGetMessage = getMessage;
        }

        public void InitializeChat(string userId)
        {
            _isStopped = false;
            new Thread(LoopChat).Start();

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
            _chatClient = null;
            _isStopped = true;
        }

        private void SetupCommands()
        {
            _commands.Add("/savePos", new Command(_ => OnSavePosition?.Invoke()));
            _commands.Add("/loadPos", new Command(_ => { OnLoadPosition?.Invoke(); }));
            _commands.Add("/spawn", new Command(RequestSpawn));
            _commands.Add("/reconnect", new Command(RequestReconnect));
            _commands.Add("/disconnect", new Command(RequestDisconnect));
            _commands.Add("/rebirth", new Command(RequestRebirth));
            _commands.Add("/giveup", new Command(RequestGiveUp));
            _commands.Add("/ready", new Command(_ => { _wukongClient.SetReadyState(true); }));
            _commands.Add("/start", new Command(_ => { _wukongClient.RequestStartPvP(); }));
        }

        private void RequestSpawn(string[] args)
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

        private void RequestRebirth(params object[] _)
        {
            OnRebirthRequested?.Invoke();
            SendServerMessage($"Player {NickName} requested rebirth");
        }

        private void RequestGiveUp(params object[] _)
        {
            SendServerMessage($"Player {NickName} gave up");
            _wukongClient.KillCurrentPlayer();
        }

        private void RequestReconnect(params object[] _)
        {
            OnReconnectRequest?.Invoke();
        }

        private void RequestDisconnect(params object[] _)
        {
            SendServerMessage($"{NickName} has left!");
            OnDisconnectRequest?.Invoke();
        }

        private void ServiceChat()
        {
            _chatClient?.Service();

            if (_onGetMessage is null)
            {
                Logging.LogWarning("Get message callback is null");
                return;
            }

            var message = _onGetMessage.Invoke();
            if (!string.IsNullOrEmpty(message))
            {
                if (!TryHandleCommand(message))
                {
                    SendChatMessage(message);
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
            Logging.LogDebug("Chat loop started");

            while (!_isStopped)
            {
                ServiceChat();
                Thread.Sleep(33);
            }

            Logging.LogDebug("Chat loop stopped");
        }

        private void SendChatMessage(string message)
        {
            if (_chatClient == null)
            {
                Logging.LogError("Chat client is null");
                return;
            }

            Logging.LogDebug("Sending message {Message}", message);
            _chatClient.PublishMessage(GeneralChannelName, $"{ClientPrefix}{message}");
        }

        public void SendServerMessage(string message)
        {
            if (_chatClient == null)
            {
                Logging.LogError("Chat client is null");
                return;
            }

            Logging.LogDebug("Sending server message {Message}", message);
            _chatClient.PublishMessage(GeneralChannelName, $"{ServerPrefix}{message}");
        }

        public void DebugReturn(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Debug: Logging.LogDebug("[Photon Chat] {Log}", message); break;
                case LogLevel.Info: Logging.LogInformation("[Photon Chat] {Log}", message); break;
                case LogLevel.Warning: Logging.LogWarning("[Photon Chat] {Log}", message); break;
                case LogLevel.Error: Logging.LogError("[Photon Chat] {Log}", message); break;
                case LogLevel.Off: break;
            }
        }

        public void OnChatStateChange(ChatState state)
        {
            Logging.LogDebug("Chat state changed to: {State}", state);
        }

        public void OnConnected()
        {
            Logging.LogDebug("Chat connected");
            _chatClient!.Subscribe(GeneralChannelName);
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