using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using Photon.Client;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Protocol;

namespace ReadyM.Relay.Client
{
    public class RelayClient : RelayPeerBase, IDisposable
    {
        private const string Host = "localhost";
        private const int Port = 9050;

        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;
        private readonly Action<LogLevel, string, object?[]> _logger;

        private Thread? _clientThread;
        private bool _isRunning;

        private Dictionary<object, object> RoomState { get; set; } = new();
        private Dictionary<object, object> PlayerState { get; set; } = new();
        private ConcurrentDictionary<int, Dictionary<object, object>> ConnectedPlayers { get; set; } = new();

        public int ActorId { get; private set; } = -1;

        private NetPeer? Server
        {
            get
            {
                if (_client.FirstPeer == null)
                {
                    Log(LogLevel.Warning, "Disconnected from server");
                }

                return _client.FirstPeer;
            }
        }

        public RelayClient(Action<LogLevel, string, object?[]> logger)
        {
            _listener = new EventBasedNetListener();
            _listener.NetworkReceiveEvent += OnListenerOnNetworkReceiveEvent;
            _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;

            _client = new NetManager(_listener)
            {
                AutoRecycle = true,
#if DEBUG
                SimulateLatency = true,
                SimulationMinLatency = 50,
                SimulationMaxLatency = 150,
#endif
            };
            _logger = logger;
        }

        public void Start()
        {
            _client.Start();
            _client.Connect(Host, Port, "Wukong"); // TODO: JWT

            _isRunning = true;
            _clientThread = new Thread(() =>
            {
                Log(LogLevel.Information, "Running relay client on port {0}", Port);
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

        public void OpSetCustomPropertiesOfActor(int playerId, Dictionary<object, object> data)
        {
            // send actor state
            var writer = new NetDataWriter();

            writer.PutEventHeader(SystemEvent.PlayerStateChanged);
            writer.Put(playerId);

            SerializeObject(writer, data);
            Server?.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod)
        {
            var writer = new NetDataWriter();
            writer.PutEventHeader(eventCode, mode);

            if (data == null)
            {
                Server?.Send(writer, deliveryMethod);
                return;
            }

            SerializeObject(writer, data);

            Server?.Send(writer, deliveryMethod);
        }

        [Obsolete]
        public void RegisterType(
            Type customType,
            byte code,
            SerializeStreamMethod serializeMethod,
            DeserializeStreamMethod deserializeMethod)
        {
            RegisterType(customType, code, (writer, customObject) =>
            {
                var stream = new StreamBuffer();
                serializeMethod(stream, customObject);
                writer.PutBytesWithLength(stream.GetBuffer());
            }, reader =>
            {
                var bytes = reader.GetBytesWithLength();
                var buffer = new StreamBuffer(bytes);
                return deserializeMethod(buffer, 0);
            });
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Stop();
        }

        private void Log(LogLevel level, [StructuredMessageTemplate] string message, params object?[] values)
        {
            _logger(level, $"[Relay Client] {message}", values);
        }

        private void OnListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliverymethod)
        {
            var (eventCode, _) = reader.GetEventHeader();

            var systemEvent = (SystemEvent)eventCode;

            switch (systemEvent)
            {
                case SystemEvent.ActorNumberAssigned:
                    ActorId = reader.GetInt();
                    Log(LogLevel.Information, "Assigned Actor ID {0}", ActorId);
                    break;
                case SystemEvent.RoomStateChanged:
                    var state = DeserializeObject(reader);
                    if (state is Dictionary<object, object> newState)
                    {
                        RoomState = newState;
                        Log(LogLevel.Information, "Room state changed");

                        foreach (var (key, value) in newState)
                        {
                            Log(LogLevel.Information, "Key: {0}, Value: {1}", key.ToString(), value.ToString());
                        }
                    }

                    break;
                case SystemEvent.PlayerStateChanged:
                    var playerId = reader.GetInt();
                    var playerState = DeserializeObject(reader);
                    if (playerState is Dictionary<object, object> newPlayerState)
                    {
                        if (playerId == ActorId)
                        {
                            PlayerState = newPlayerState;
                        }
                        else
                        {
                            ConnectedPlayers.AddOrUpdate(playerId, newPlayerState, (_, _) => newPlayerState);
                        }

                        Log(LogLevel.Information, "Player {0} state changed", playerId);

                        foreach (var (key, value) in newPlayerState)
                        {
                            Log(LogLevel.Information, "Key: {0}, Value: {1}", key.ToString(), value.ToString());
                        }
                    }

                    break;
            }
        }

        private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            Log(LogLevel.Debug, "Network latency updated: {0}ms", latency);
        }
    }
}