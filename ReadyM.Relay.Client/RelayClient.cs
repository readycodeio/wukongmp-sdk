using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using Photon.Client;
using ReadyM.Relay.Common.Protocol;
using DeserializeStreamMethod = ReadyM.Relay.Client.Serialization.DeserializeStreamMethod;
using SerializeStreamMethod = ReadyM.Relay.Client.Serialization.SerializeStreamMethod;

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

        private readonly Dictionary<Type, (byte Code, SerializeStreamMethod Serialize, DeserializeStreamMethod Deserialize)> _registeredTypes = new();
        private readonly Dictionary<byte, (Type Type, SerializeStreamMethod Serialize, DeserializeStreamMethod Deserialize)> _code2Type = new();

        public RelayClient()
        {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            Configure();
            RegisterDefaultTypes();
        }

        private void RegisterDefaultTypes()
        {
            // primitives
            RegisterType(typeof(byte), 1, (stream, customObject) =>
            {
                stream.WriteByte((byte)customObject);
                return 1;
            }, (stream, _) => stream.ReadByte());

            RegisterType(typeof(short), 2, (stream, customObject) =>
            {
                stream.Write(BitConverter.GetBytes((short)customObject), 0, 2);
                return 2;
            }, (stream, _) =>
            {
                var bytes = new byte[2];
                stream.Read(bytes, 0, 2);
                return BitConverter.ToInt16(bytes, 0);
            });

            RegisterType(typeof(int), 3, (stream, customObject) =>
            {
                stream.Write(BitConverter.GetBytes((int)customObject), 0, 4);
                return 4;
            }, (stream, _) =>
            {
                var bytes = new byte[4];
                stream.Read(bytes, 0, 4);
                return BitConverter.ToInt32(bytes, 0);
            });

            RegisterType(typeof(long), 4, (stream, customObject) =>
            {
                stream.Write(BitConverter.GetBytes((long)customObject), 0, 8);
                return 8;
            }, (stream, _) =>
            {
                var bytes = new byte[8];
                stream.Read(bytes, 0, 8);
                return BitConverter.ToInt64(bytes, 0);
            });

            RegisterType(typeof(float), 5, (stream, customObject) =>
            {
                stream.Write(BitConverter.GetBytes((float)customObject), 0, 4);
                return 4;
            }, (stream, _) =>
            {
                var bytes = new byte[4];
                stream.Read(bytes, 0, 4);
                return BitConverter.ToSingle(bytes, 0);
            });

            RegisterType(typeof(double), 6, (stream, customObject) =>
            {
                stream.Write(BitConverter.GetBytes((double)customObject), 0, 8);
                return 8;
            }, (stream, _) =>
            {
                var bytes = new byte[8];
                stream.Read(bytes, 0, 8);
                return BitConverter.ToDouble(bytes, 0);
            });

            RegisterType(typeof(string), 7, (stream, customObject) =>
            {
                var str = (string)customObject;
                var bytes = Encoding.UTF8.GetBytes(str);
                stream.Write(BitConverter.GetBytes((short)bytes.Length), 0, 2);
                stream.Write(bytes, 0, bytes.Length);
                return (short)(2 + bytes.Length);
            }, (stream, _) =>
            {
                var lengthBytes = new byte[2];
                stream.Read(lengthBytes, 0, 2);
                var strLength = BitConverter.ToInt16(lengthBytes, 0);

                var strBytes = new byte[strLength];
                stream.Read(strBytes, 0, strLength);
                return Encoding.UTF8.GetString(strBytes);
            });

            // arrays of primitives
            RegisterType(typeof(byte[]), 8, (stream, customObject) =>
            {
                var arr = (byte[])customObject;
                stream.Write(BitConverter.GetBytes((short)arr.Length), 0, 2);
                stream.Write(arr, 0, arr.Length);
                return (short)(2 + arr.Length);
            }, (stream, _) =>
            {
                var lengthBytes = new byte[2];
                stream.Read(lengthBytes, 0, 2);
                var arrLength = BitConverter.ToInt16(lengthBytes, 0);

                var arr = new byte[arrLength];
                stream.Read(arr, 0, arrLength);
                return arr;
            });

            RegisterType(typeof(int[]), 9, (stream, customObject) =>
            {
                var arr = (int[])customObject;
                stream.Write(BitConverter.GetBytes((short)arr.Length), 0, 2);
                foreach (var i in arr)
                {
                    stream.Write(BitConverter.GetBytes(i), 0, 4);
                }

                return (short)(2 + arr.Length * 4);
            }, (stream, _) =>
            {
                var lengthBytes = new byte[2];
                stream.Read(lengthBytes, 0, 2);
                var arrLength = BitConverter.ToInt16(lengthBytes, 0);

                var arr = new int[arrLength];
                for (var i = 0; i < arrLength; i++)
                {
                    var bytes = new byte[4];
                    stream.Read(bytes, 0, 4);
                    arr[i] = BitConverter.ToInt32(bytes, 0);
                }

                return arr;
            });

            // photon hashtable
            RegisterType(typeof(PhotonHashtable), 10, (stream, customObject) =>
            {
                var hashtable = (PhotonHashtable)customObject;

                stream.Write(BitConverter.GetBytes((short)hashtable.Count), 0, 2);
                short totalSize = 2;

                foreach (var (key, value) in hashtable)
                {
                    totalSize += SerializeObject(stream, key);
                    totalSize += SerializeObject(stream, value);
                }

                return totalSize;
            }, (stream, _) =>
            {
                var lengthBytes = new byte[2];
                stream.Read(lengthBytes, 0, 2);
                var hashtableLength = BitConverter.ToInt16(lengthBytes, 0);

                var hashtable = new PhotonHashtable();
                for (var i = 0; i < hashtableLength; i++)
                {
                    var key = DeserializeObject(stream)!;
                    var value = DeserializeObject(stream);

                    hashtable.Add(key, value);
                }

                return hashtable;
            });
        }

        private void Configure()
        {
            _listener.NetworkReceiveEvent += OnListenerOnNetworkReceiveEvent;
        }

        private void OnListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliverymethod)
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
            if (_code2Type.TryGetValue(code, out var value))
            {
                throw new ArgumentException($"Code {code} is already registered for type {value.Type}");
            }

            _registeredTypes[customType] = (code, serializeMethod, deserializeMethod);
            _code2Type[code] = (customType, serializeMethod, deserializeMethod);
        }

        public void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod)
        {
            var writer = new NetDataWriter();

            if (data == null)
            {
                // send without data
                writer.PutBytesWithLength([eventCode, (byte)mode, 0]);
                _client.SendToAll(writer, deliveryMethod);
                return;
            }

            if (!_registeredTypes.TryGetValue(data.GetType(), out var typeInfo))
            {
                throw new ArgumentException($"Type {data.GetType()} is not registered");
            }

            var dataBuffer = new StreamBuffer();
            SerializeObject(dataBuffer, data);

            writer.Put(eventCode);
            writer.Put((byte)mode);
            writer.PutBytesWithLength(dataBuffer.GetBuffer());

            _client.SendToAll(writer, deliveryMethod);
        }

        private short SerializeObject(
            StreamBuffer stream,
            object? data)
        {
            if (data == null)
            {
                stream.WriteByte(0);
                return 1;
            }

            if (!_registeredTypes.TryGetValue(data.GetType(), out var typeInfo))
            {
                throw new ArgumentException($"Type {data.GetType()} is not registered");
            }

            stream.WriteByte(typeInfo.Code);

            return (short)(1 + typeInfo.Serialize(stream, data));
        }

        private object? DeserializeObject(StreamBuffer stream)
        {
            var typeCode = stream.ReadByte();
            if (typeCode == 0)
            {
                return null;
            }

            if (!_code2Type.TryGetValue(typeCode, out var typeInfo))
            {
                throw new ArgumentException($"Type code {typeCode} is not registered");
            }

            return typeInfo.Deserialize(stream, 0); // length is unused
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Stop();
        }
    }
}