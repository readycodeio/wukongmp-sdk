using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
        public bool Ready => _client.IsConnectedAndReady;

        private readonly Action _joinedRoomCallback;
        public event Action<int, float, float, float> OnPlayerJoined;
        public event Action<int, float[]> OnPlayerPosition;
        public event Action<int, byte, string, float, float, float> OnUnitSpawn;
        public event Action<int, KeyPress> OnKeyReceived;
        public event Action<int, byte> OnRollSkill;
        public event Action<int, byte, float, float> OnJumpSkillCue;

        private const string UserName = "ReadyM_julkiewicz";
        public WukongChatter WukongChat => _wukongChat;

        private readonly float _initialX;
        private readonly float _initialY;
        private readonly float _initialZ;

        public PlayerState LocalPlayerState { get; }
        public readonly Dictionary<int, PlayerState> ConnectedPlayers = new Dictionary<int, PlayerState>();

        public PlayerState GetByActor(AActor actor)
        {
            var kvp = ConnectedPlayers.FirstOrDefault(x => x.Value.Pawn == actor);
            return kvp.Value;
        }

        public WukongClient(Action onJoinedRoom, float x, float y, float z)
        {
            _joinedRoomCallback = onJoinedRoom;
            _initialX = x;
            _initialY = y;
            _initialZ = z;

            LocalPlayerState = new PlayerState(_client.LocalPlayer.ActorNumber, null);
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
                    var pos = (float[])photonEvent.CustomData;
                    OnPlayerJoined?.Invoke(photonEvent.Sender, pos[0], pos[1], pos[2]);
                    break;
                }
                case 1:
                {
                    // position update
                    var posAndRot = (float[])photonEvent.CustomData;
                    OnPlayerPosition?.Invoke(photonEvent.Sender, posAndRot);
                    break;
                }
                case 2:
                    // key press
                    var key = (KeyPress)photonEvent.CustomData;
                    OnKeyReceived?.Invoke(photonEvent.Sender, key);
                    break;
                case 3:
                    // key press
                    var unitData = (UnitSpawnData)photonEvent.CustomData;
                    OnUnitSpawn?.Invoke(photonEvent.Sender, unitData.Id, unitData.Name, unitData.X, unitData.Y, unitData.Z);
                    break;
                case 4:
                    // roll skill
                    OnRollSkill?.Invoke(photonEvent.Sender, (byte)photonEvent.CustomData);
                    break;
                case 5:
                    // jump skill cue
                    var jumpSkillCue = (byte[])photonEvent.CustomData;
                    var startJumpDir = jumpSkillCue[0];
                    var currentInputX = BitConverter.ToSingle(jumpSkillCue, 1);
                    var currentInputY = BitConverter.ToSingle(jumpSkillCue, 5);
                    OnJumpSkillCue?.Invoke(photonEvent.Sender, startJumpDir, currentInputX, currentInputY);
                    break;
            }
        }

        public void StartClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            PhotonPeer.RegisterType(typeof(KeyPress), 255, KeyPress.Serialize, KeyPress.Deserialize);
            PhotonPeer.RegisterType(typeof(UnitSpawnData), 254, UnitSpawnData.Serialize, UnitSpawnData.Deserialize);

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
            var evData = new[] { _initialX, _initialY, _initialZ };
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
            _wukongChat.InitializeChat(UserName);
        }

        public void SendPositionUpdate(float x, float y, float z, float rx, float ry, float rz, float rw)
        {
            const byte eventCode = 1;
            var evData = new[] { x, y, z, rx, ry, rz, rw };
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendKeyPressed(PlayerInput key, KeyState state)
        {
            var press = new KeyPress(key, state);
            const byte eventCode = 2;
            _client.OpRaiseEvent(eventCode, press, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SpawnUnit(byte id, string unitName, float x, float y, float z)
        {
            const byte eventCode = 3;
            var evData = new UnitSpawnData(id, unitName, x, y, z);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendRollSkill(byte rollDir)
        {
            const byte eventCode = 4;
            _client.OpRaiseEvent(eventCode, rollDir, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendJumpSkillCue(byte startjumpdir, float currentinputX, float currentinputY)
        {
            const byte eventCode = 5;
            var evData = new List<byte> { startjumpdir };
            evData.AddRange(BitConverter.GetBytes(currentinputX));
            evData.AddRange(BitConverter.GetBytes(currentinputY));

            _client.OpRaiseEvent(eventCode, evData.ToArray(), RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        private readonly PhotonHashtable _playerProperties = new PhotonHashtable();

        public void SendUpdatedPlayerProperties()
        {
            if (_playerProperties.Count == 0)
                return;

            _client.OpSetCustomPropertiesOfActor(Id, _playerProperties);
            _playerProperties.Clear();
        }

        public void SendIsFlying(bool isFlying)
        {
#if LOCAL_TESTING
            foreach (var (id, state) in ConnectedPlayers)
            {
                state.IsFlying = isFlying;
                Helpers.Log($"Assigned IsFlying ({isFlying}) to player {id}");
            }
#else
            _playerProperties[nameof(PlayerState.IsFlying)] = isFlying;
#endif
        }

        public void SendIsFalling(bool isFalling)
        {
#if LOCAL_TESTING
            foreach (var (id, state) in ConnectedPlayers)
            {
                state.IsFalling = isFalling;
                Helpers.Log($"Assigned IsFalling ({isFalling}) to player {id}");
            }

#else
            _playerProperties[nameof(PlayerState.IsFalling)] = isFalling;
#endif
        }

        public void SendIsLandingMove(bool lastIsLandingMove)
        {
#if LOCAL_TESTING
            foreach (var (id, state) in ConnectedPlayers)
            {
                state.IsLandingMove = lastIsLandingMove;
                Helpers.Log($"Assigned IsLandingMove ({lastIsLandingMove}) to player {id}");
            }
#else
            _playerProperties[nameof(PlayerState.IsLandingMove)] = lastIsLandingMove;
#endif
        }

        public void SendVelocity(FVector velocity)
        {
#if LOCAL_TESTING
            foreach (var (id, state) in ConnectedPlayers)
            {
                state.Velocity = velocity;
                Helpers.Log($"Assigned Velocity ({velocity}) to player {id}");
            }
#else
            _playerProperties[nameof(PlayerState.Velocity)] = velocity;
#endif
        }

        public void SendMoveAcceleration(FVector moveAcceleration)
        {
#if LOCAL_TESTING
            foreach (var (id, state) in ConnectedPlayers)
            {
                state.MoveAcceleration = moveAcceleration;
                Helpers.Log($"Assigned MoveAcceleration ({moveAcceleration}) to player {id}");
            }
#else
            _playerProperties[nameof(PlayerState.MoveAcceleration)] = moveAcceleration;
#endif
        }

        public void SendInJump(bool inJump)
        {
#if LOCAL_TESTING
            foreach (var (id, state) in ConnectedPlayers)
            {
                state.InJump = inJump;
                Helpers.Log($"Assigned InJump ({inJump}) to player {id}");
            }
#else
            _playerProperties[nameof(PlayerState.InJump)] = inJump;
#endif
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

        public void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged) { }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            var id = targetPlayer.ActorNumber;

            if (targetPlayer.IsLocal || !ConnectedPlayers.TryGetValue(id, out var playerState))
            {
                Helpers.Log($"Player {id} is local or not found.");
                return;
            }

            if (changedProps.TryGetValue(nameof(PlayerState.IsFlying), out var isFlying))
            {
                playerState.IsFlying = (bool)isFlying;
                Helpers.Log($"Assigned IsFlying ({isFlying}) to player {id}");
            }

            if (changedProps.TryGetValue(nameof(PlayerState.IsFalling), out var isFalling))
            {
                playerState.IsFalling = (bool)isFalling;
                Helpers.Log($"Assigned IsFalling ({isFalling}) to player {id}");
            }

            if (changedProps.TryGetValue(nameof(PlayerState.IsLandingMove), out var isLandingMove))
            {
                playerState.IsLandingMove = (bool)isLandingMove;
                Helpers.Log($"Assigned IsLandingMove ({isLandingMove}) to player {id}");
            }

            if (changedProps.TryGetValue(nameof(PlayerState.Velocity), out var velocity))
            {
                playerState.Velocity = (FVector)velocity;
                Helpers.Log($"Assigned Velocity ({velocity}) to player {id}");
            }

            if (changedProps.TryGetValue(nameof(PlayerState.MoveAcceleration), out var moveAcceleration))
            {
                playerState.MoveAcceleration = (FVector)moveAcceleration;
                Helpers.Log($"Assigned MoveAcceleration ({moveAcceleration}) to player {id}");
            }

            if (changedProps.TryGetValue(nameof(PlayerState.InJump), out var inJump))
            {
                playerState.InJump = (bool)inJump;
                Helpers.Log($"Assigned InJump ({inJump}) to player {id}");
            }
        }

        public void OnMasterClientSwitched(Player newMasterClient) { }
    }
}