using System;
using System.Collections.Generic;
using System.Threading;
using Photon.Chat;
using Photon.Client;

namespace WukongMp.Common
{
    public class WukongChatter : IChatClientListener
    {
        private ChatClient _chatClient;

        private const string GeneralChannelName = "General";
        private const string ServerChannelName = "Server";
        private string _userName;
        private Thread _bgChatThread;

        public event Func<string> OnGetMessage;
        public event Action<bool, string, string> OnSendMessage;

        public void InitializeChat(string userName)
        {
            _userName = userName;
            _chatClient = new ChatClient(this);
            _chatClient.Connect("d4af67fe-a776-499e-8f56-f169d3db616e", "1.0", new AuthenticationValues(userName));

            _bgChatThread = new Thread(LoopChat);
            _bgChatThread.Start();

            Console.WriteLine("\n\nYou are: " + userName);
        }

        private void ServiceChat()
        {
            _chatClient.Service();
            var message = OnGetMessage?.Invoke();
            if (!string.IsNullOrEmpty(message))
            {
                SendChatMessage(GeneralChannelName, message);
            }
        }

        private void LoopChat(object state)
        {
            while (true)
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

        public void DebugReturn(LogLevel level, string message)
        {
        }

        public void OnChatStateChange(ChatState state)
        {
        }

        public void OnConnected()
        {
            Console.WriteLine("Chat connected");
            _chatClient.Subscribe(GeneralChannelName);
            _chatClient.Subscribe(ServerChannelName);
            SendChatMessage(ServerChannelName, $"{_userName} has joined!");
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

        public void OnPrivateMessage(string sender, object message, string channelName)
        {
        }

        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
        }

        public void OnSubscribed(string[] channels, bool[] results)
        {
            for (var i = 0; i < channels.Length; i++)
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