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
        private bool _quit;
        private Thread _bgThread;

        private int Id => _client.LocalPlayer.ActorNumber;
        public bool Ready => _client.IsConnectedAndReady;

        public event Action<int> OnPlayerJoined;
        public event Action<int, float, float, float> OnPlayerMoved;
        public event Action<int, KeyPress> OnKeyReceived;

        ~WukongClient()
        {
            _client.Disconnect();
            _client.RemoveCallbackTarget(this);
        }

#if UNITY_EDITOR
        private void Log(string message) {
            UnityEngine.Debug.Log(message);
        }
#else
        private void Log(string message)
        {
            Console.WriteLine(message);
        }
#endif

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Sender == Id)
                return;

            Log($"Received message from {photonEvent.Sender}: {photonEvent.Code}");

            switch (photonEvent.Code)
            {
                case 0:
                    // room joined
                    OnPlayerJoined?.Invoke(photonEvent.Sender);
                    break;
                case 1:
                    // position update
                    var pos = (float[])photonEvent.CustomData;
                    OnPlayerMoved?.Invoke(photonEvent.Sender, pos[0], pos[1], pos[2]);
                    break;
                case 2:
                    // key press
                    var key = (KeyPress)photonEvent.CustomData;
                    OnKeyReceived?.Invoke(photonEvent.Sender, key);
                    break;
            }
        }

        public void StartClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            PhotonPeer.RegisterType(typeof(KeyPress), 255, KeyPress.Serialize, KeyPress.Deserialize);

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

            _bgThread = new Thread(Loop);
            _bgThread.Start();

            Log("Running forever.");
        }

        public void Reconnect()
        {
            _client.ReconnectAndRejoin();
        }

        private void Loop(object state)
        {
            while (!_quit)
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
                if (!player.Value.IsLocal)
                    yield return player.Value.ActorNumber;
            }
        }

        private void MyJoinRandomOrCreateRoom()
        {
            var propertiesForRoomCreation = new RoomOptions
            {
                PublishUserId = true,
            };
            var enterRoomParams = new EnterRoomArgs
            {
                RoomOptions = propertiesForRoomCreation
            };

            var joinRoomParams = new JoinRandomRoomArgs();

            _client.OpJoinRandomOrCreateRoom(joinRoomParams, enterRoomParams);
        }

        private void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Log(arg1 + " -> " + arg2);
        }

        private void SendRoomJoined()
        {
            const byte eventCode = 0; // make up event codes at will, < 200
            _client.OpRaiseEvent(eventCode, null, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendKeyPressed(PlayerInput key, KeyState state)
        {
            var press = new KeyPress(key, state);
            const byte eventCode = 2; // make up event codes at will, < 200
            _client.OpRaiseEvent(eventCode, press, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void SendPositionUpdate(float x, float y, float z)
        {
            const byte eventCode = 1; // make up event codes at will, < 200
            var evData = new float[] { x, y, z };

            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        #region IConnectionCallbacks

        public void OnConnected()
        {
            Log("OnConnected");
        }

        public void OnConnectedToMaster()
        {
            Log("OnConnectedToMaster Server: " + _client.RealtimePeer.ServerIpAddress);
            MyJoinRandomOrCreateRoom();
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Log($"OnDisconnected: {cause}");
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Log("OnRegionListReceived");
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Log("OnCustomAuthenticationResponse");

            foreach (var kvp in data)
            {
                Log($"{kvp.Key}: {kvp.Value}");
            }
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Log("OnCustomAuthenticationFailed: " + debugMessage);
        }

        #endregion

        #region IMatchmakingCallbacks

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            Log("OnFriendListUpdate");
        }

        public void OnCreatedRoom()
        {
            Log("OnCreatedRoom");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Log("OnCreateRoomFailed: " + message);
        }

        public void OnJoinedRoom()
        {
            Log("OnJoinedRoom");
            SendRoomJoined();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Log("OnJoinRoomFailed: " + message);
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Log("OnJoinRandomFailed: " + message);
        }

        public void OnLeftRoom()
        {
            Log("OnLeftRoom");
        }

        #endregion
    }
}