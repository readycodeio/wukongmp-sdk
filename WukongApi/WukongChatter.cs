using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Photon.Chat;
using Photon.Client;
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

        private const string GeneralChannelName = "General";
        private const string ServerChannelName = "Server";
        private string _userName;

        private bool _isExit;

        public event Func<string> OnGetMessage;
        public event Action<bool, string, string> OnSendMessage;

        public event Action OnSavePosition;
        public event Action OnLoadPosition;
        public event Action OnConnectRequest;
        public event Action OnReconnectRequest;
        public event Action OnDisconnectRequest;
        public event Action OnEnablePvP;
        public event Action OnRebirthRequested;
        public event Action<string, int, int> OnSpawnEnemy;

        private const char Separator = ' ';
        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

        public WukongChatter(WukongClient owner)
        {
            _wukongClient = owner;
            SetupCommands();

            new Thread(LoopChat).Start();
        }

        public void InitializeChat(string userName)
        {
            _userName = userName;
            _chatClient = new ChatClient(this);
            _chatClient.Connect("d4af67fe-a776-499e-8f56-f169d3db616e", "1.0", new AuthenticationValues(userName));

            Console.WriteLine("\n\nYou are: " + userName);
        }

        public void Disconnect()
        {
            _chatClient?.Disconnect();
        }

        public void StopMessageService()
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
                    Handler = args => { OnSavePosition?.Invoke(); }
                });

            _commands.Add(
                "/loadPos",
                new Command
                {
                    Name = "Load checkpoint",
                    Handler = args => { OnLoadPosition?.Invoke(); }
                });

            _commands.Add(
                "/spawn",
                new Command
                {
                    Name = "Spawn enemy NPC",
                    Handler = args =>
                    {
                        // if name number, then pass, else 1
                        switch (args.Length)
                        {
                            case 1:
                                OnSpawnEnemy?.Invoke(args[0], 1, _wukongClient.LocalPlayerState.TeamId);
                                SendChatMessage(ServerChannelName, "Spawned monster");
                                break;
                            case 2:
                            {
                                if (int.TryParse(args[1], out var count))
                                {
                                    OnSpawnEnemy?.Invoke(args[0], count, _wukongClient.LocalPlayerState.TeamId);
                                    SendChatMessage(ServerChannelName, $"Spawned {count} monsters");
                                }

                                break;
                            }
                        }
                    }
                });
            _commands.Add(
                "/connect",
                new Command
                {
                    Name = "Connect",
                    Handler = _ => { RequestConnect(); }
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
                "/pvp",
                new Command
                {
                    Name = "Enable PvP",
                    Handler = _ => { EnablePvP(); }
                });
            _commands.Add(
                "/rebirth",
                new Command
                {
                    Name = "Rebirth",
                    Handler = _ => { RequestRebirth(); }
                });
        }

        private void RequestRebirth()
        {
            OnRebirthRequested?.Invoke();
            SendChatMessage(ServerChannelName, $"Player {_userName} requested rebirth");
        }

        private void EnablePvP()
        {
            OnEnablePvP?.Invoke();
            SendChatMessage(ServerChannelName, $"Player {_userName} enabled PvP (team: {_wukongClient.LocalPlayerState.TeamId - Constants.BaseTeamId})");
        }

        public void RequestConnect()
        {
            OnConnectRequest?.Invoke();
        }

        public void RequestReconnect()
        {
            OnReconnectRequest?.Invoke();
        }

        private void RequestDisconnect()
        {
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
                        SendChatMessage(GeneralChannelName, message);
                    }
                }
            }
        }

        private bool TryHandleCommand(string message)
        {
            string[] commandParts = message.Split(Separator);
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

        private void SendChatMessage(string channel, string message)
        {
            Console.WriteLine($"Sending message {message}");
            _chatClient.PublishMessage(channel, message);
        }

        public void DebugReturn(LogLevel level, string message) { }

        public void OnChatStateChange(ChatState state)
        {
            Console.WriteLine($"Chat state changed to: {state}");
        }

        public void OnConnected()
        {
            Console.WriteLine("Chat connected");
            _chatClient.Subscribe(GeneralChannelName);
            _chatClient.Subscribe(ServerChannelName);
            SendChatMessage(ServerChannelName, $"{_userName} has joined!");
        }

        public void OnCustomAuthenticationFailed(string debugMessage) { }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }

        public void OnDisconnected()
        {
            Console.WriteLine("Chat disconnected");
            SendChatMessage(ServerChannelName, $"{_userName} has left!");
        }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            for (var i = 0; i < senders.Length; i++)
            {
                Console.WriteLine($"Message {messages[i]} recieved");
                if (channelName == ServerChannelName)
                {
                    OnSendMessage?.Invoke(true, "Server", messages[i].ToString());
                }
                else
                {
                    OnSendMessage?.Invoke(false, senders[i], messages[i].ToString());
                }
            }
        }

        public void OnPrivateMessage(string sender, object message, string channelName) { }

        public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }

        public void OnSubscribed(string[] channels, bool[] results)
        {
            for (var i = 0; i < channels.Length; i++)
            {
                Console.WriteLine($"Subscribed to the channel: {channels[i]}: {results[i]}");
            }
        }

        public void OnUnsubscribed(string[] channels) { }

        public void OnUserSubscribed(string channel, string user) { }

        public void OnUserUnsubscribed(string channel, string user) { }
    }
}