using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Photon.Chat;
using Photon.Client;
using WukongApi.Patches;
using WukongApi.UI;
using AuthenticationValues = Photon.Chat.AuthenticationValues;

namespace WukongApi
{
    internal class Command(Action<ReadOnlyMemory<string>> handler)
    {
        public Action<ReadOnlyMemory<string>> Handler { get; } = handler;
    }

    public class WukongChatter : IChatClientListener
    {
        private readonly ChatClient _chatClient;
        private readonly WukongClient _wukongClient;

        private const string ServerPrefix = "<S>";
        private const string ClientPrefix = "<C>";
        private string RoomName => _wukongClient.PhotonClient.CurrentRoom?.Name ?? Guid.NewGuid().ToString(); // do not collide with anybody if sth goes wrong
        private string GeneralChannelName => $"chat-${RoomName}";
        private string NickName => _wukongClient.LocalPlayerState.NickName;

        private bool _isStopped = true;

        private const char Separator = ' ';
        private readonly Dictionary<string, Command> _commands = new();

        public WukongChatter(WukongClient owner)
        {
            _wukongClient = owner;
            _chatClient = new ChatClient(this);
            SetupCommands();
        }

        public void StartClient(string userId)
        {
            var authValues = new AuthenticationValues(userId)
            {
                AuthType = CustomAuthenticationType.Custom,
            };
            authValues.AddAuthParameter("access_token", CmdLineParams.Instance.AccessToken);

            _chatClient.AuthValues = authValues;

            _isStopped = false;
            new Thread(LoopChat).Start();

            _chatClient.ConnectUsingSettings(new ChatAppSettings
            {
                AppIdChat = Constants.ChatAppId,
                AppVersion = "1.0",
                FixedRegion = "us",
            });
        }

        public void StopClient()
        {
            Logging.LogInformation("Chat client disconnecting");
            _chatClient.Unsubscribe([GeneralChannelName]);
            _chatClient.Disconnect();
            _isStopped = true;
            Logging.LogInformation("Chat client stopped");
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
            _wukongClient.StopPhotonClient();
        }

        private void ServiceChat()
        {
            _chatClient.Service();
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
            Logging.LogDebug("Sending message {Message}", message);
            _chatClient.PublishMessage(GeneralChannelName, $"{ClientPrefix}{message}");
        }

        public void SendServerMessage(string message)
        {
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
                ChatWidget.Instance.AddMessage(isServer, isServer ? "Server" : senders[i], message);
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