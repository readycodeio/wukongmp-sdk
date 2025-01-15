using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using b1;
using CSharpModBase;
using Photon.Client;
using Photon.Realtime;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public class WukongClient : IConnectionCallbacks, IOnEventCallback, IMatchmakingCallbacks, IInRoomCallbacks
    {
        private readonly RealtimeClient _client = new RealtimeClient();
        private readonly WukongChatter _wukongChat = new WukongChatter();

        private int Id => _client.LocalPlayer.ActorNumber;
        public bool IsMasterClient => _client.CurrentRoom.MasterClientId == Id;
        public bool Ready => _client.IsConnectedAndReady;

        private readonly Action _joinedRoomCallback;
        public event Action<int> OnPlayerJoined;
        public event Action<int, MontageCallbackData> OnMontageCallback;
        public event Action<int, MonsterMontageCallbackData> OnMonsterMontageCallback;
        public event Action<int, int, string, float, float, float> OnUnitSpawn;

        private const string UserName = Constants.PhotonUserName;
        public WukongChatter WukongChat => _wukongChat;

        public PlayerState LocalPlayerState { get; }
        public readonly Dictionary<int, PlayerState> ConnectedPlayers = new Dictionary<int, PlayerState>();
        public readonly Dictionary<int, MonsterState> SyncedMonsters = new Dictionary<int, MonsterState>();

        public PlayerState GetByActor(AActor actor)
        {
            var kvp = ConnectedPlayers.FirstOrDefault(x => x.Value.Pawn == actor);
            return kvp.Value;
        }

        public MonsterState GetByTamerActor(BUTamerActor owner)
        {
            var kvp = SyncedMonsters.FirstOrDefault(x => x.Value.Pawn == owner);
            return kvp.Value;
        }

        public MonsterState GetMonsterByCharacter(BGUCharacterCS owner)
        {
            var kvp = SyncedMonsters.FirstOrDefault(x => x.Value.Pawn.GetMonster() == owner);
            return kvp.Value;
        }

        public WukongClient(Action onJoinedRoom)
        {
            _joinedRoomCallback = onJoinedRoom;
            LocalPlayerState = new PlayerState(_client.LocalPlayer.ActorNumber, GameUtils.GetControlledPawn());
        }

        ~WukongClient()
        {
            _client.Disconnect();
            _client.RemoveCallbackTarget(this);
        }

        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case 0:
                {
                    // room joined
                    OnPlayerJoined?.Invoke(photonEvent.Sender);
                    break;
                }
                case 1:
                    // unit spawn
                    var unitData = (UnitSpawnData)photonEvent.CustomData;
                    OnUnitSpawn?.Invoke(photonEvent.Sender, unitData.Id, unitData.Name, unitData.X, unitData.Y, unitData.Z);
                    break;
                case 2:
                    // montage callback
                    var montData = (MontageCallbackData)photonEvent.CustomData;
                    OnMontageCallback?.Invoke(photonEvent.Sender, montData);
                    break;
                case 3:
                    // monster properties
                    ApplyMonsterMove(photonEvent.CustomData as PhotonHashtable);
                    break;
                case 4:
                    // montage callback
                    var monsterMontageData = (MonsterMontageCallbackData)photonEvent.CustomData;
                    OnMonsterMontageCallback?.Invoke(photonEvent.Sender, monsterMontageData);
                    break;
            }
        }

        public void StartClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            PhotonPeer.RegisterType(typeof(UnitSpawnData), 255, UnitSpawnData.Serialize, UnitSpawnData.Deserialize);
            PhotonPeer.RegisterType(typeof(FVector), 254, (stream, obj) =>
            {
                var vec = (FVector)obj;
                stream.Write(BitConverter.GetBytes(vec.X), 0, 4);
                stream.Write(BitConverter.GetBytes(vec.Y), 0, 4);
                stream.Write(BitConverter.GetBytes(vec.Z), 0, 4);
                return 12;
            }, (stream, length) =>
            {
                var floatBytes = new byte[4];
                stream.Read(floatBytes, 0, 4);
                var x = BitConverter.ToSingle(floatBytes, 0);
                stream.Read(floatBytes, 0, 4);
                var y = BitConverter.ToSingle(floatBytes, 0);
                stream.Read(floatBytes, 0, 4);
                var z = BitConverter.ToSingle(floatBytes, 0);
                return new FVector(x, y, z);
            });
            PhotonPeer.RegisterType(typeof(FRotator), 253, (stream, obj) =>
            {
                var vec = (FRotator)obj;
                stream.Write(BitConverter.GetBytes(vec.Pitch), 0, 4);
                stream.Write(BitConverter.GetBytes(vec.Yaw), 0, 4);
                stream.Write(BitConverter.GetBytes(vec.Roll), 0, 4);
                return 12;
            }, (stream, length) =>
            {
                var floatBytes = new byte[4];
                stream.Read(floatBytes, 0, 4);
                var pitch = BitConverter.ToSingle(floatBytes, 0);
                stream.Read(floatBytes, 0, 4);
                var yaw = BitConverter.ToSingle(floatBytes, 0);
                stream.Read(floatBytes, 0, 4);
                var roll = BitConverter.ToSingle(floatBytes, 0);
                return new FRotator(pitch, yaw, roll);
            });
            PhotonPeer.RegisterType(typeof(EMoveSpeedLevel), 252, (stream, obj) =>
            {
                stream.WriteByte((byte)obj);
                return 1;
            }, (stream, length) => (EMoveSpeedLevel)stream.ReadByte());

            PhotonPeer.RegisterType(typeof(MontageCallbackData), 251, MontageCallbackData.Serialize, MontageCallbackData.Deserialize);
            PhotonPeer.RegisterType(typeof(MonsterMontageCallbackData), 250, MonsterMontageCallbackData.Serialize, MonsterMontageCallbackData.Deserialize);

            _client.AddCallbackTarget(this);
            _client.StateChanged += OnStateChange;

            _client.ConnectUsingSettings(new AppSettings
            {
                AppIdRealtime = "4fefdae2-db02-446c-bd5b-382a8ff41c08",
                FixedRegion = "eu",
                Protocol = ConnectionProtocol.WebSocket,
                EnableProtocolFallback = false,
                AuthMode = AuthModeOption.AuthOnce
            });

            new Thread(LoopGame).Start();
            Helpers.Log("Running forever.");
        }

        // ReSharper disable once FunctionNeverReturns
        private void LoopGame(object state)
        {
            while (true)
            {
                _client.Service();
                Thread.Sleep(33);
            }
        }

        public IEnumerable<int> GetOtherPlayersInRoom()
        {
            if (_client.CurrentRoom is null)
            {
                Helpers.Log("No room joined.");
                yield break;
            }

            foreach (var player in _client.CurrentRoom.Players)
            {
                Helpers.Log($"Other player: {player.Value.ActorNumber} {player.Value.UserId} local: {player.Value.IsLocal}");
                if (!player.Value.IsLocal)
                    yield return player.Value.ActorNumber;
            }
        }

        private void MyJoinRandomOrCreateRoom()
        {
            var propertiesForRoomCreation = new RoomOptions
            {
                PublishUserId = true
            };
            var enterRoomParams = new EnterRoomArgs
            {
                RoomOptions = propertiesForRoomCreation,
                RoomName = "Kuba123123"
            };

            _client.OpJoinOrCreateRoom(enterRoomParams);
        }

        private void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Helpers.Log(arg1 + " -> " + arg2);
        }

        private void SendRoomJoined()
        {
            const byte eventCode = 0;
            _client.OpRaiseEvent(eventCode, null, RaiseEventArgs.Default, SendOptions.SendReliable);
            _wukongChat.InitializeChat(UserName);
        }

        public void SpawnUnit(int id, string unitName, float x, float y, float z)
        {
            const byte eventCode = 1;
            var evData = new UnitSpawnData(id, unitName, x, y, z);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendMontageCallback(EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            const byte eventCode = 2;
            var evData = new MontageCallbackData(reason, montagePath, state);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendMonsterMontageCallback(int monsterId, EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            const byte eventCode = 4;
            var evData = new MonsterMontageCallbackData(monsterId, reason, montagePath, state);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        private void ApplyMonsterMove(PhotonHashtable props)
        {
            foreach (var (key, value) in props)
            {
                var compositeKey = (string)key;
                var parts = compositeKey.Split('_');
                if (parts.Length != 2)
                {
                    Helpers.Log($"Invalid key: {compositeKey}");
                    continue;
                }

                var id = int.Parse(parts[0]);
                var propName = parts[1];

                if (!SyncedMonsters.TryGetValue(id, out var monsterState))
                {
                    Helpers.Log($"Monster {id} not found.");
                    continue;
                }

                if (!MonsterSetters.TryGetValue(propName, out var setter))
                {
                    setter = CreateSetter<MonsterState>(propName);
                    MonsterSetters[propName] = setter;
                }

                setter(monsterState, value);
            }
        }

        private ConcurrentDictionary<string, object> _playerProperties = new ConcurrentDictionary<string, object>();

        private ConcurrentDictionary<string, object> _playerPropertiesRo = new ConcurrentDictionary<string, object>();

        private readonly object _playerPropertiesLock = new object();

        public void SendUpdatedPlayerProperties()
        {
            lock (_playerPropertiesLock)
            {
                (_playerProperties, _playerPropertiesRo) = (_playerPropertiesRo, _playerProperties);

                if (_playerPropertiesRo.Count == 0)
                    return;

                var hashtable = new PhotonHashtable();
                foreach (var (key, value) in _playerPropertiesRo)
                {
                    hashtable[key] = value;
                }

                _playerPropertiesRo.Clear();
                _client.OpSetCustomPropertiesOfActor(Id, hashtable);
            }
        }

        public void SetPlayerProperty(string key, object value)
        {
            _playerProperties[key] = value;

            if (!(value is FVector || value is FRotator || key == nameof(PlayerState.TurnInplaceRemainAngle)))
            {
                Helpers.Log($"SetPlayerProperty: {key} = {value}");
            }
        }

        public void SendRemotePlayerProperty(int playerId, string key, object value)
        {
            if (!IsMasterClient)
            {
                Helpers.Log("Only master client can send remote player properties.");
                return;
            }

            var hashtable = new PhotonHashtable
            {
                [key] = value
            };

            Helpers.Log($"Sending remote player property: {key} = {value}");

            Utils.TryRunOnGameThread(() => { _client.OpSetCustomPropertiesOfActor(playerId, hashtable); });
        }

        private ConcurrentDictionary<string, object> _monsterProperties = new ConcurrentDictionary<string, object>();

        private ConcurrentDictionary<string, object> _monsterPropertiesRo = new ConcurrentDictionary<string, object>();

        private readonly object _monsterPropertiesLock = new object();

        public void SendUpdatedMonsterProperties()
        {
            lock (_monsterPropertiesLock)
            {
                (_monsterProperties, _monsterPropertiesRo) = (_monsterPropertiesRo, _monsterProperties);

                if (_monsterPropertiesRo.Count == 0)
                    return;

                var hashtable = new PhotonHashtable();
                foreach (var (key, value) in _monsterPropertiesRo)
                {
                    hashtable[key] = value;
                }

                _monsterPropertiesRo.Clear();

                const byte eventCode = 3;
                _client.OpRaiseEvent(eventCode, hashtable, RaiseEventArgs.Default, SendOptions.SendUnreliable);
            }
        }

        public void SetMonsterProperty(int id, string prop, object value)
        {
            _monsterProperties[$"{id}_{prop}"] = value;

            if (!(value is FVector || value is FRotator))
            {
                Helpers.Log($"SetMonsterProperty [{id}]: {prop} = {value}");
            }
        }

        #region IConnectionCallbacks

        public void OnConnected()
        {
            Helpers.Log("Connected");
        }

        public void OnConnectedToMaster()
        {
            Helpers.Log("Connected to master server: " + _client.RealtimePeer.ServerIpAddress);
            MyJoinRandomOrCreateRoom();
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Helpers.Log($"Disconnected: {cause}");
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Helpers.Log("Region list received");
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Helpers.Log("Custom authentication response");

            foreach (var kvp in data)
            {
                Helpers.Log($"{kvp.Key}: {kvp.Value}");
            }
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Helpers.Log("Custom authentication failed: " + debugMessage);
        }

        #endregion

        #region IMatchmakingCallbacks

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            Helpers.Log("Friend list update");
        }

        public void OnCreatedRoom()
        {
            Helpers.Log("Created room");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Helpers.Log("Create room failed: " + message);
        }

        public void OnJoinedRoom()
        {
            Helpers.Log("Joined room");

            MyMod.Instance.Harmony.PatchCategory(Assembly.GetExecutingAssembly(), Constants.RoomPatches);
            Helpers.Log("Patched with Harmony");

            _joinedRoomCallback?.Invoke();
            SendRoomJoined();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Helpers.Log("Join room failed: " + message);
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Helpers.Log("Join random failed: " + message);
        }

        public void OnLeftRoom()
        {
            Helpers.Log("Left room");
        }

        #endregion

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            Helpers.Log($"Player {newPlayer.UserId} entered the room");
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Helpers.Log($"Player {otherPlayer.UserId} left the room");
        }

        public void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
        {
            // nothing
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            var id = targetPlayer.ActorNumber;

            PlayerState playerState;

            if (targetPlayer.IsLocal)
            {
                playerState = LocalPlayerState;
            }
            else if (!ConnectedPlayers.TryGetValue(id, out playerState))
            {
                Helpers.Log($"Player {id} not found.");
                return;
            }

            foreach (var kvp in changedProps)
            {
                var propertyName = (string)kvp.Key;

                if (!PlayerSetters.TryGetValue(propertyName, out var setter))
                {
                    setter = CreateSetter<PlayerState>(propertyName);
                    PlayerSetters[propertyName] = setter;
                }

                if (!(kvp.Value is FVector || kvp.Value is FRotator || kvp.Value is float))
                {
                    Helpers.Log($"Assigning {propertyName} = {kvp.Value} to player {id}");
                }

                setter(playerState, kvp.Value);
            }
        }

        public void OnMasterClientSwitched(Player newMasterClient) { }

        private static readonly Dictionary<string, Action<PlayerState, object>> PlayerSetters = new Dictionary<string, Action<PlayerState, object>>();
        private static readonly Dictionary<string, Action<MonsterState, object>> MonsterSetters = new Dictionary<string, Action<MonsterState, object>>();

        private static Action<T, object> CreateSetter<T>(string propertyName)
        {
            var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new InvalidOperationException($"Property '{propertyName}' not found on {typeof(T).Name}.");

            // Create the lambda (T state, object value) => state.Property = (T)value;
            var stateParam = Expression.Parameter(typeof(T), "state");
            var valueParam = Expression.Parameter(typeof(object), "value");

            // Cast value to the correct type
            var convertedValue = Expression.Convert(valueParam, property.PropertyType);

            // Build the assignment: state.Property = (T)value;
            var body = Expression.Assign(Expression.Property(stateParam, property), convertedValue);

            // Compile the lambda expression
            return Expression.Lambda<Action<T, object>>(body, stateParam, valueParam).Compile();
        }
    }
}