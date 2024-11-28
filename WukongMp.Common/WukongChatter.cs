using Photon.Chat;
using Photon.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WukongMp.Common
{
    public class WukongChatter : IChatClientListener
    {
        private ChatClient _chatClient;

        private const string generalChannelName = "General";
        private const string serverChannelName = "Server";
        private string _userName;

        public event Func<string> OnGetMessage;
        public event Action<bool, string, string> OnSendMessage;

        public void InitializeChat(string userName)
        {
            _userName = userName;
            _chatClient = new ChatClient(this);
            _chatClient.Connect("d4af67fe-a776-499e-8f56-f169d3db616e", "1.0", new AuthenticationValues(userName));

            Console.WriteLine("\n\nYou are: " + userName);
        }

        public void ServiceChat()
        {
            if (_chatClient != null)
            {
                _chatClient.Service();
                var message = OnGetMessage?.Invoke();
                if (message != null && message.Length > 0)
                {
                    SendChatMessage(generalChannelName, message);
                }
            }
        }

        private void SendChatMessage(string channel, string message)
        {
            if (_chatClient != null)
            {
                Console.WriteLine($"Sending message {message}");
                _chatClient.PublishMessage(channel, message);
            }
        }

        public void DebugReturn(LogLevel level, string message)
        {
        }

        public void OnChatStateChange(ChatState state)
        {
        }

        public void OnConnected()
        {
            Console.WriteLine("Chat connected");
            _chatClient.Subscribe(generalChannelName);
            _chatClient.Subscribe(serverChannelName);
            SendChatMessage(serverChannelName, $"{_userName} has joined!");
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
        }

        public void OnDisconnected()
        {
            Console.WriteLine("Chat disconnected");
            SendChatMessage(serverChannelName, $"{_userName} has left!");
        }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            for (int i = 0; i < senders.Length; i++)
            {
                Console.WriteLine($"Message {messages[i]} recieved");
                if (channelName == serverChannelName)
                {
                    OnSendMessage?.Invoke(true, "Server", messages[i].ToString());
                }
                else
                {
                    OnSendMessage?.Invoke(false, senders[i], messages[i].ToString());
                }
            }
        }

        public void OnPrivateMessage(string sender, object message, string channelName)
        {
        }

        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
        }

        public void OnSubscribed(string[] channels, bool[] results)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                Console.WriteLine($"Subscribed to the channel: {channels[i]}: {results[i]}");
            }
        }

        public void OnUnsubscribed(string[] channels)
        {
        }

        public void OnUserSubscribed(string channel, string user)
        {
        }

        public void OnUserUnsubscribed(string channel, string user)
        {
        }
    }
}
