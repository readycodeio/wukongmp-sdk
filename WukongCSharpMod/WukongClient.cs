using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using b1;
using Photon.Client;
using Photon.Realtime;
using UnrealEngine.Engine;
using WukongCSharpMod;

namespace WukongMp.Common
{
    public class WukongClient : IConnectionCallbacks, IOnEventCallback, IMatchmakingCallbacks
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

#if UNITY_EDITOR
        private static void Log(string message) {
            UnityEngine.Debug.Log(message);
        }
#else
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }
#endif

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
                case 6:
                    var isFalling = (bool)photonEvent.CustomData;
                    var sender = photonEvent.Sender;

                    if (ConnectedPlayers.TryGetValue(sender, out var playerState))
                    {
                        playerState.LastIsFalling = isFalling;
                        Log($"Received message: Player {sender} is falling: {isFalling}");
                    }
                    else
                    {
                        Log($"Received message: Player {sender} is falling: {isFalling} (not found)");

                        // assign to all connnected players
                        foreach (var player in GetOtherPlayersInRoom())
                        {
                            if (ConnectedPlayers.TryGetValue(player, out playerState))
                            {
                                playerState.LastIsFalling = isFalling;
                            }
                        }
                    }

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
            Log("Running forever.");
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
                Log("No room joined.");
                yield break;
            }

            foreach (var player in _client.CurrentRoom.Players)
            {
                Log($"Other player: {player.Value.ActorNumber} {player.Value.UserId} local: {player.Value.IsLocal}");
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
            Log(arg1 + " -> " + arg2);
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

        public void SendRollSkill(byte rolldir)
        {
            const byte eventCode = 4;
            var evData = rolldir;
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendJumpSkillCue(byte startjumpdir, float currentinputX, float currentinputY)
        {
            const byte eventCode = 5;
            var evData = new List<byte> { startjumpdir };
            evData.AddRange(BitConverter.GetBytes(currentinputX));
            evData.AddRange(BitConverter.GetBytes(currentinputY));

            _client.OpRaiseEvent(eventCode, evData.ToArray(), RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendIsFalling(bool isFalling)
        {
            const byte eventCode = 6;
            _client.OpRaiseEvent(eventCode, isFalling, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        #region IConnectionCallbacks

        public void OnConnected()
        {
            Log("Connected");
        }

        public void OnConnectedToMaster()
        {
            Log("Connected to master server: " + _client.RealtimePeer.ServerIpAddress);
            MyJoinRandomOrCreateRoom();
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Log($"Disconnected: {cause}");
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Log("Region list received");
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Log("Custom authentication response");

            foreach (var kvp in data)
            {
                Log($"{kvp.Key}: {kvp.Value}");
            }
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Log("Custom authentication failed: " + debugMessage);
        }

        #endregion

        #region IMatchmakingCallbacks

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            Log("Friend list update");
        }

        public void OnCreatedRoom()
        {
            Log("Created room");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Log("Create room failed: " + message);
        }

        public void OnJoinedRoom()
        {
            Log("Joined room");
            _joinedRoomCallback?.Invoke();
            SendRoomJoined();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Log("Join room failed: " + message);
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Log("Join random failed: " + message);
        }

        public void OnLeftRoom()
        {
            Log("Left room");
        }

        #endregion
    }
}