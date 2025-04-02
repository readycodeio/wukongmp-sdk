using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using ReadyM.Relay.Client.Serialization;

namespace ReadyM.Relay.Client
{
    public class RelayClient : IDisposable
    {
        private const string Host = "localhost";
        private const int Port = 9050;

        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;

        private Thread? _clientThread;
        private bool _isRunning;

        private readonly Dictionary<Type, (byte Code, SerializeStreamMethod Serialize, DeserializeStreamMethod Deserialize)> _registeredTypes
            = new Dictionary<Type, (byte Code, SerializeStreamMethod Serialize, DeserializeStreamMethod Deserialize)>();

        public RelayClient()
        {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);
            Configure();
        }

        private void Configure()
        {
            _listener.NetworkReceiveEvent += OnListenerOnNetworkReceiveEvent;
        }

        private void OnListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            Console.WriteLine("We got: {0}", reader.GetString(100 /* max length of string */));
            reader.Recycle();
        }

        public void Start()
        {
            _client.Start();
            _client.Connect(Host, Port, "Wukong"); // TODO: JWT

            _isRunning = true;
            _clientThread = new Thread(() =>
            {
                Console.WriteLine("Running client on port {0}", Port);
                while (_isRunning)
                {
                    _client.PollEvents();
                    Thread.Sleep(15);
                }
            });

            _clientThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _client.Stop();
            _clientThread?.Join();
            _clientThread = null;
        }

        public void RegisterType(
            Type customType,
            byte code,
            SerializeStreamMethod serializeMethod,
            DeserializeStreamMethod deserializeMethod)
        {
            // check if already registered
            if (_registeredTypes.ContainsKey(customType))
            {
                throw new ArgumentException($"Type {customType} is already registered");
            }

            // check if any other type has the same code, if so - throw
            foreach (var registeredType in _registeredTypes)
            {
                if (registeredType.Value.Code == code)
                {
                    throw new ArgumentException($"Code {code} is already registered for type {registeredType.Key}");
                }
            }

            _registeredTypes[customType] = (code, serializeMethod, deserializeMethod);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Stop();
        }
    }
}