using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Photon.Client;
using Photon.Realtime;

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
        public event Action<int, float, float, float, float, bool> OnAttackRotation;
        public event Action<int, byte> OnRollSkill;
        public event Action<int, bool> OnMarkRolling;
        public event Action<int> OnRestartCombo;
        public event Action<int, int, int> OnChangeDodgeSkill;
        public event Action<int> OnResetDodgeSkill;
        public event Action<int, byte, float, float> OnJumpSkillCue;
        public event Action<int, float> OnStrideJump;
        public event Action<int, byte> OnSwitchFsmSolver;
        public event Action<int, float> OnUpdateFsmSolver;
        public event Action<int, string> OnFsmEvent;

        private const string UserName = "ReadyM_julkiewicz";
        public WukongChatter WukongChat => _wukongChat;

        private readonly float _initialX;
        private readonly float _initialY;
        private readonly float _initialZ;

        public WukongClient(Action onJoinedRoom, float x, float y, float z)
        {
            _joinedRoomCallback = onJoinedRoom;
            _initialX = x;
            _initialY = y;
            _initialZ = z;
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
                    // attack rotation
                    var attackRotation = (float[])photonEvent.CustomData;
                    OnAttackRotation?.Invoke(photonEvent.Sender, attackRotation[0], attackRotation[1], attackRotation[2], attackRotation[3], attackRotation[4] > 0);
                    break;
                case 5:
                    // roll skill
                    OnRollSkill?.Invoke(photonEvent.Sender, (byte)photonEvent.CustomData);
                    break;
                case 6:
                    // mark rolling
                    OnMarkRolling?.Invoke(photonEvent.Sender, (bool)photonEvent.CustomData);
                    break;
                case 7:
                    // restart combo
                    OnRestartCombo?.Invoke(photonEvent.Sender);
                    break;
                case 8:
                    // change dodge skill
                    var dodgeSkill = (int[])photonEvent.CustomData;
                    OnChangeDodgeSkill?.Invoke(photonEvent.Sender, dodgeSkill[0], dodgeSkill[1]);
                    break;
                case 9:
                    // reset dodge skill
                    OnResetDodgeSkill?.Invoke(photonEvent.Sender);
                    break;
                case 10:
                    // jump skill cue
                    var jumpSkillCue = (byte[])photonEvent.CustomData;
                    var startJumpDir = jumpSkillCue[0];
                    var currentInputX = BitConverter.ToSingle(jumpSkillCue, 1);
                    var currentInputY = BitConverter.ToSingle(jumpSkillCue, 5);
                    OnJumpSkillCue?.Invoke(photonEvent.Sender, startJumpDir, currentInputX, currentInputY);
                    break;
                case 11:
                    // stride jump
                    OnStrideJump?.Invoke(photonEvent.Sender, (float)photonEvent.CustomData);
                    break;
                case 12:
                    // switch fsm solver
                    OnSwitchFsmSolver?.Invoke(photonEvent.Sender, (byte)photonEvent.CustomData);
                    break;
                case 13:
                    // update fsm solver
                    OnUpdateFsmSolver?.Invoke(photonEvent.Sender, (float)photonEvent.CustomData);
                    break;
                case 14:
                    // fsm event
                    OnFsmEvent?.Invoke(photonEvent.Sender, (string)photonEvent.CustomData);
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

        public void Reconnect()
        {
            _client.ReconnectAndRejoin();
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
                RoomName = "Kuba123"
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

        public void SendKeyPressed(PlayerInput key, KeyState state)
        {
            var press = new KeyPress(key, state);
            const byte eventCode = 2;
            _client.OpRaiseEvent(eventCode, press, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendPositionUpdate(float x, float y, float z, float rx, float ry, float rz, float rw)
        {
            const byte eventCode = 1;
            var evData = new[] { x, y, z, rx, ry, rz, rw };
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SpawnUnit(byte id, string unitName, float x, float y, float z)
        {
            const byte eventCode = 3;
            var evData = new UnitSpawnData(id, unitName, x, y, z);
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendAttackRotation(float x, float y, float z, float turnspeed, bool force)
        {
            const byte eventCode = 4;
            var evData = new[] { x, y, z, turnspeed, force ? 1 : 0 };
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendRollSkill(byte rolldir)
        {
            const byte eventCode = 5;
            var evData = rolldir;
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendMarkRolling(bool p1)
        {
            const byte eventCode = 6;
            var evData = p1;
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }


        public void SendReStartCombo()
        {
            const byte eventCode = 7;
            _client.OpRaiseEvent(eventCode, null, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendChangeDodgeSkill(int p1, int p2)
        {
            const byte eventCode = 8;
            var evData = new[] { p1, p2 };
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendResetDodgeSkill()
        {
            const byte eventCode = 9;
            _client.OpRaiseEvent(eventCode, null, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendJumpSkillCue(byte startjumpdir, float currentinputX, float currentinputY)
        {
            const byte eventCode = 10;
            var evData = new List<byte> { startjumpdir };
            evData.AddRange(BitConverter.GetBytes(currentinputX));
            evData.AddRange(BitConverter.GetBytes(currentinputY));

            _client.OpRaiseEvent(eventCode, evData.ToArray(), RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendStrideJump(float height)
        {
            const byte eventCode = 11;
            var evData = height;
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendSwitchFsmSolver(byte newsolvertype)
        {
            const byte eventCode = 12;
            var evData = newsolvertype;
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendUpdateFsmSolver(float p1)
        {
            const byte eventCode = 13;
            var evData = p1;
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendFsmEvent(string toString)
        {
            const byte eventCode = 14;
            var evData = toString;
            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
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