using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using b1;
using BtlB1;
using BtlShare;
using CSharpModBase;
using Photon.Client;
using Photon.Realtime;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.Patches;
using WukongApi.State;
using PlayerState = WukongApi.State.PlayerState;

namespace WukongApi
{
    public class WukongClient : IConnectionCallbacks, IOnEventCallback, IMatchmakingCallbacks, IInRoomCallbacks
    {
        private readonly RealtimeClient _client = new RealtimeClient();
        private readonly string _userName;
        private const char MonsterHashtableKeySeparator = ';';

        private bool _isExit;

        protected int PhotonId => _client.LocalPlayer.ActorNumber;
        public bool IsMasterClient => _client.CurrentRoom?.MasterClientId == PhotonId;
        public bool Ready => _client.IsConnectedAndReady;

        private readonly Action _joinedRoomCallback;
        private readonly Action<Player> _playerJoinedCallback;
        public event Action<int, MontageCallbackData> OnMontageCallback;
        public event Action<int, MonsterMontageCallbackData> OnMonsterMontageCallback;
        public event Action<int, string, string, int, float, float, float> OnUnitSpawn;
        public event Action<string> OnMonsterWakeUp;
        public event Action<int, EquipmentState> OnEquipmentChange;
        public event Action<int> OnPlayerRebirth;
        public event Action<int> OnKillPlayer;
        public event Action OnBeforeJoinRoom;
        public event Action<DamageNumParam> OnDamageNum;

        public WukongChatter WukongChat { get; }
        public LobbyManager LobbyManager { get; private set; }

        public PlayerState LocalPlayerState { get; protected set; }

        public readonly Dictionary<int, PlayerState> ConnectedPlayers = new Dictionary<int, PlayerState>();
        public readonly Dictionary<string, MonsterState> SyncedMonsters = new Dictionary<string, MonsterState>();

        private readonly List<WukongClientClone> _photonClones = new List<WukongClientClone>();

        public void RegisterPlayer(PlayerState state)
        {
            Logging.LogDebug($"Registering player {state.PhotonId}");
            ConnectedPlayers.Add(state.PhotonId, state);
        }

        public PlayerState GetByActor(AActor actor)
        {
            if (actor == LocalPlayerState.Pawn)
                return LocalPlayerState;
            var kvp = ConnectedPlayers.FirstOrDefault(x => x.Value.Pawn == actor);
            return kvp.Value;
        }

        public APawn GetPlayerPawn(int playerId)
        {
            if (playerId == LocalPlayerState.PhotonId)
                return LocalPlayerState.Pawn;
            else if (ConnectedPlayers.TryGetValue(playerId, out var playerState))
                return playerState.Pawn;
            return null;
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

        public void RemoveMonster(string monsterGuid)
        {
            SyncedMonsters.Remove(monsterGuid);
        }

        public WukongClient(string userName, Action onJoinedRoom, Action<Player> playerJoinedCallback)
        {
            WukongChat = new WukongChatter(this);
            _userName = userName;
            _joinedRoomCallback = onJoinedRoom;
            _playerJoinedCallback = playerJoinedCallback;
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
                case 1:
                    // unit spawn
                    var unitData = (UnitSpawnData)photonEvent.CustomData;
                    OnUnitSpawn?.Invoke(photonEvent.Sender, unitData.Guid, unitData.Name, unitData.TeamId, unitData.X, unitData.Y, unitData.Z);
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
                case 5:
                    // monster wake up
                    var guid = (string)photonEvent.CustomData;
                    OnMonsterWakeUp?.Invoke(guid);
                    break;
                case 6:
                    // damage num
                    var damageNumParam = (DamageNumParam)photonEvent.CustomData;
                    OnDamageNum?.Invoke(damageNumParam);
                    break;
                case 7:
                    // player rebirth
                    var playerId = (int)photonEvent.CustomData;
                    OnPlayerRebirth?.Invoke(playerId);
                    break;
                case 8:
                    // start countdown
                    GameUtils.ShowPvPCountDown();
                    WukongMP.Instance.EnablePvP();
                    break;
                case 9:
                    // readiness signal
                    var isReady = (bool)photonEvent.CustomData;
                    OnPlayerReadinessChanged(photonEvent.Sender, isReady);
                    break;
                case 10:
                    // kill player
                    var id = (int)photonEvent.CustomData;
                    OnKillPlayer?.Invoke(id);
                    break;
            }
        }

        private void OnPlayerReadinessChanged(int playerId, bool isReady)
        {
            LobbyManager.DisplayReadinessChangeTips();

            if (IsMasterClient) // send this only once
            {
                var player = _client.CurrentRoom.Players[playerId];
                WukongChat.SendChatMessage(WukongChatter.ServerChannelName, $"{player.NickName} is {(isReady ? "ready" : "not ready")}");
            }
        }

        public void StartClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            PhotonPeer.RegisterType(typeof(UnitSpawnData), 255, UnitSpawnData.Serialize, UnitSpawnData.Deserialize);
            PhotonPeer.RegisterType(typeof(FVector), 254, SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);
            PhotonPeer.RegisterType(typeof(FRotator), 253, SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
            PhotonPeer.RegisterType(typeof(EMoveSpeedLevel), 252, (stream, obj) =>
            {
                stream.WriteByte((byte)obj);
                return 1;
            }, (stream, length) => (EMoveSpeedLevel)stream.ReadByte());

            PhotonPeer.RegisterType(typeof(MontageCallbackData), 251, MontageCallbackData.Serialize, MontageCallbackData.Deserialize);
            PhotonPeer.RegisterType(typeof(MonsterMontageCallbackData), 250, MonsterMontageCallbackData.Serialize, MonsterMontageCallbackData.Deserialize);
            PhotonPeer.RegisterType(typeof(EquipmentState), 249, EquipmentState.Serialize, EquipmentState.Deserialize);
            PhotonPeer.RegisterType(typeof(DamageNumParam), 248, SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);

            _client.AddCallbackTarget(this);
            _client.StateChanged += OnStateChange;

            OnBeforeJoinRoom?.Invoke();

            _client.ConnectUsingSettings(new AppSettings
            {
                AppIdRealtime = "4fefdae2-db02-446c-bd5b-382a8ff41c08",
                FixedRegion = "eu",
                Protocol = ConnectionProtocol.WebSocket,
                EnableProtocolFallback = false,
                AuthMode = AuthModeOption.AuthOnce
            });

            new Thread(LoopGame).Start();

            Logging.LogDebug("Running forever.");
        }

        public void StopClient()
        {
            Logging.LogDebug("Stopping client...");

            _isExit = true;

            WukongChat.Disconnect();
            _client.Disconnect();

            Logging.LogDebug("Stopped client.");

            _client.RemoveCallbackTarget(this);

            // unpatch harmony
            Utils.TryRunOnGameThread(() =>
            {
                WukongMP.Instance.Harmony.UnpatchCategory(Constants.ConnectedPatches);
                Logging.LogDebug("Unpatched Harmony");
            });

            // destroy all connected players
            foreach (var player in ConnectedPlayers.Values)
            {
                BGU_UnrealWorldUtil.DestroyActor(player.Pawn);
            }
        }

        // ReSharper disable once FunctionNeverReturns
        private void LoopGame()
        {
            while (!_isExit)
            {
                _client.Service();
                Thread.Sleep(33);
            }
        }

        public void SpawnClone()
        {
            var clone = new WukongClientClone();
            _photonClones.Add(clone);

            clone.StartClient();
        }

        public IEnumerable<Player> GetOtherPlayersInRoom()
        {
            if (_client.CurrentRoom is null)
            {
                Logging.LogWarning("No room joined.");
                yield break;
            }

            foreach (var player in _client.CurrentRoom.Players)
            {
                Logging.LogDebug($"Other player: {player.Value.ActorNumber} {player.Value.UserId} local: {player.Value.IsLocal}");
                if (!player.Value.IsLocal)
                    yield return player.Value;
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
                RoomName = "KubaCloneTest"
            };

            _client.OpJoinOrCreateRoom(enterRoomParams);
        }

        private void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Logging.LogDebug(arg1 + " -> " + arg2);
        }

        public void SpawnUnit(string id, string unitName, int teamID, float x, float y, float z)
        {
            const byte eventCode = 1;
            var evData = new UnitSpawnData(id, unitName, teamID, x, y, z);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendMontageCallback(EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            const byte eventCode = 2;
            var evData = new MontageCallbackData(reason, montagePath, state);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);

            foreach (var clone in _photonClones)
            {
                clone.SendMontageCallback(reason, montagePath, state);
            }
        }

        public void SendMonsterMontageCallback(string monsterId, EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            const byte eventCode = 4;
            var evData = new MonsterMontageCallbackData(monsterId, reason, montagePath, state);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendMonsterWakeUp(string guid)
        {
            const byte eventCode = 5;
            _client.OpRaiseEvent(eventCode, guid, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendDamageNum(DamageNumParam damageNumParam)
        {
            const byte eventCode = 6;
            _client.OpRaiseEvent(eventCode, damageNumParam, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void RebirthCurrentPlayer()
        {
            const byte eventCode = 7;
            _client.OpRaiseEvent(eventCode, PhotonId, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.All
            }, SendOptions.SendUnreliable);
        }

        public void SendStartCountdown()
        {
            if (!IsMasterClient)
            {
                Logging.LogError("Only room owner can send start countdown.");
                return;
            }

            const byte eventCode = 8;
            _client.OpRaiseEvent(eventCode, null, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.All
            }, SendOptions.SendReliable);
        }

        public void SignalReadiness(bool isReady)
        {
            const byte eventCode = 9;
            _client.OpRaiseEvent(eventCode, isReady, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.All,
            }, SendOptions.SendReliable);
        }

        public void KillCurrentPlayer()
        {
            const byte eventCode = 10;
            _client.OpRaiseEvent(eventCode, PhotonId, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.MasterClient,
            }, SendOptions.SendReliable);
        }

        public void CacheEquipmentChange(EquipPosition position, int newEq)
        {
            LocalPlayerState.Equipment.SetEquipment(position, newEq);
            CachePlayerProperty(nameof(PlayerState.Equipment), LocalPlayerState.Equipment);
        }

        public void StartPvP()
        {
            if (!IsMasterClient)
            {
                GameUtils.ShowTip("Only room owner can start PvP.");
                return;
            }

            LobbyManager.StartRound();
        }

        protected virtual void ApplyMonsterMove(PhotonHashtable props)
        {
            foreach (var (key, value) in props)
            {
                var compositeKey = (string)key;
                var parts = compositeKey.Split(MonsterHashtableKeySeparator);
                if (parts.Length != 2)
                {
                    Logging.LogDebug($"Invalid key: {compositeKey}");
                    continue;
                }

                var guid = parts[0];
                var propName = parts[1];

                if (!SyncedMonsters.TryGetValue(guid, out var monsterState))
                {
                    Logging.LogDebug($"Monster {guid} not found.");
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

        public void SetCachedPlayerProperties()
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
                _client.LocalPlayer.SetCustomProperties(hashtable);
            }

            foreach (var clone in _photonClones)
            {
                clone.SetCachedPlayerProperties();
            }
        }

        public virtual void CachePlayerProperty(string key, object value)
        {
            _playerProperties[key] = value;
            if (!(value is FVector || value is FRotator || key == nameof(PlayerState.TurnInplaceRemainAngle)))
            {
                Logging.LogDebug($"Set player property: {key} = {value}");
            }

            foreach (var clone in _photonClones)
            {
                clone.CachePlayerProperty(key, value);
            }
        }

        public void CachePlayerAttribute(EBGUAttrFloat attr, float value)
        {
            CachePlayerProperty($"{Constants.AttributePrefix}{attr}", value);
        }

        public void SetRemotePlayerProperty(int playerId, string key, object value)
        {
            if (!IsMasterClient)
            {
                Logging.LogDebug("Only room owner can send remote player properties.");
                return;
            }

            var hashtable = new PhotonHashtable
            {
                [key] = value
            };

            Logging.LogDebug($"Sending remote player property: {key} = {value}");

            _client.OpSetCustomPropertiesOfActor(playerId, hashtable);
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

        public void CacheMonsterProperty(string guid, string prop, object value)
        {
            _monsterProperties[$"{guid}{MonsterHashtableKeySeparator}{prop}"] = value;

            if (!(value is FVector || value is FRotator))
            {
                Logging.LogDebug($"Set monster property [{guid}]: {prop} = {value}");
            }
        }

        #region IConnectionCallbacks

        public void OnConnected()
        {
            Logging.LogDebug("Connected");
        }

        public void OnConnectedToMaster()
        {
            Logging.LogDebug("Connected to master server: " + _client.RealtimePeer.ServerIpAddress);
            MyJoinRandomOrCreateRoom();
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Logging.LogDebug($"Disconnected: {cause}");
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Logging.LogDebug("Region list received");
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Logging.LogDebug("Custom authentication response");

            foreach (var kvp in data)
            {
                Logging.LogDebug($"{kvp.Key}: {kvp.Value}");
            }
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Logging.LogDebug("Custom authentication failed: " + debugMessage);
        }

        #endregion

        #region IMatchmakingCallbacks

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            Logging.LogDebug("Friend list update");
        }

        public void OnCreatedRoom()
        {
            Logging.LogDebug("Created room");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Logging.LogDebug("Create room failed: " + message);
        }

        public virtual void OnJoinedRoom()
        {
            Logging.LogDebug("Joined room");

            _client.NickName = _userName;

            var teamId = PhotonUtils.GetTeamIdForPlayer(PhotonId);
            LocalPlayerState = new PlayerState(PhotonId, GameUtils.GetControlledPawn(), teamId);

            if (IsMasterClient)
            {
                LobbyManager = new LobbyManager(this);
                LobbyManager.PlayerTeleportRequested += OnPlayerTeleportRequested;
            }

            Utils.TryRunOnGameThread(() =>
            {
                WukongMP.Instance.Harmony.PatchCategory(Constants.ConnectedPatches);
                Logging.LogDebug("Patched with Harmony");
            });

            _joinedRoomCallback?.Invoke();
            WukongChat.InitializeChat(_userName);

            GameLoopPatch.QueueOnGameThread(PhotonUtils.DiscoverMonsters, "DiscoverMonsters");
        }

        private void OnPlayerTeleportRequested(Action continuation)
        {
            // TODO: Move players to appropriate positions
            continuation();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Logging.LogDebug("Join room failed: " + message);
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Logging.LogDebug("Join random failed: " + message);
        }

        public void OnLeftRoom()
        {
            Logging.LogDebug("Left room");

            Utils.TryRunOnGameThread(() =>
            {
                WukongMP.Instance.Harmony.UnpatchCategory(Constants.ConnectedPatches);
                Logging.LogDebug("Unpatched Harmony");
            });
        }

        #endregion

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            Logging.LogDebug($"Player {newPlayer.ActorNumber} entered the room");
            _playerJoinedCallback?.Invoke(newPlayer);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Logging.LogDebug($"Player {otherPlayer.ActorNumber} left the room");

            BGU_UnrealWorldUtil.DestroyActor(ConnectedPlayers[otherPlayer.ActorNumber].Pawn);
            ConnectedPlayers.Remove(otherPlayer.ActorNumber);
        }

        public void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
        {
            // nothing
        }

        public virtual void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            var id = targetPlayer.ActorNumber;

            PlayerState playerState;

            if (targetPlayer.IsLocal)
            {
                playerState = LocalPlayerState;
            }
            else if (!ConnectedPlayers.TryGetValue(id, out playerState))
            {
                Logging.LogDebug($"Player {id} not found.");
                return;
            }

            foreach (var kvp in changedProps)
            {
                if (!(kvp.Key is string propertyName))
                {
                    if (kvp.Key is byte numId && numId == ActorProperties.NickName)
                    {
                        playerState.NickName = (string)kvp.Value;
                    }
                    else
                    {
                        Logging.LogWarning($"Unhandled player state key: {kvp.Key}");
                    }

                    continue;
                }

                // attributes have special treatment
                if (propertyName.StartsWith(Constants.AttributePrefix))
                {
                    Logging.LogDebug($"Assigning {propertyName} = {kvp.Value} for player {id}");

                    var key = propertyName.Substring(Constants.AttributePrefix.Length);

                    if (!Enum.TryParse<EBGUAttrFloat>(key, out var attr))
                        throw new InvalidOperationException($"Failed to parse attribute key: {key}");

                    playerState.Attributes[attr] = (float)kvp.Value;
                    continue;
                }

                if (!PlayerSetters.TryGetValue(propertyName, out var setter))
                {
                    setter = CreateSetter<PlayerState>(propertyName);
                    PlayerSetters[propertyName] = setter;
                }

                if (!(kvp.Value is FVector || kvp.Value is FRotator || kvp.Value is float))
                {
                    Logging.LogDebug($"Assigning {propertyName} = {kvp.Value} to player {id}");
                }

                setter(playerState, kvp.Value);

                if (propertyName == nameof(PlayerState.Equipment))
                {
                    OnEquipmentChange?.Invoke(id, (EquipmentState)kvp.Value);
                }
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