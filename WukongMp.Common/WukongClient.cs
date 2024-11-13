using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Photon.Client;
using Photon.Realtime;

namespace WukongMp.Common
{
    public class WukongClient : IConnectionCallbacks, IOnEventCallback
    {
        private readonly RealtimeClient _client = new RealtimeClient();
        private bool _quit;

        ~WukongClient()
        {
            _client.Disconnect();
            _client.RemoveCallbackTarget(this);
        }

        public int Id => _client.LocalPlayer.ActorNumber;

        public event Action<int, float, float, float> OnPlayerMoved;

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Sender == Id)
                return;

            Console.WriteLine($"Received message from {photonEvent.Sender}");

            switch (photonEvent.Code)
            {
                case 1:
                    var pos = (float[])photonEvent.CustomData;
                    OnPlayerMoved?.Invoke(photonEvent.Sender, pos[0], pos[1], pos[2]);
                    break;
            }
        }

        public void StartClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

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

            var t = new Thread(Loop);
            t.Start();

            Console.WriteLine("Running forever.");
            // Console.ReadKey();
            // quit = true;
        }

        private void Loop(object state)
        {
            while (!_quit)
            {
                _client.Service();
                Thread.Sleep(33);
            }
        }

        // key of our "map type" room property
        private static string MapProperty = "m";

        // room properties available in matchmaking
        private static string[] RoomPropsInLobby = { "m" };

        // user choice, e.g. types 1 - 9
        private byte selectedMapType = 2;

        void MyJoinRandomOrCreateRoom()
        {
            // custom room properties to use when this client creates a room.
            var mapSelectionAsProperties = new PhotonHashtable { { MapProperty, selectedMapType } };

            // if a new room gets created, this sets the map property and makes it available in matchmaking
            var propertiesForRoomCreation = new RoomOptions
            {
                CustomRoomProperties = mapSelectionAsProperties,
                CustomRoomPropertiesForLobby = RoomPropsInLobby
            };
            var enterRoomParams = new EnterRoomArgs
            {
                RoomOptions = propertiesForRoomCreation
            };

            // this defines the join random filter. rooms must match the key-values in this hashtable
            var joinRoomParams = new JoinRandomRoomArgs
            {
                ExpectedCustomRoomProperties = mapSelectionAsProperties
            };

            _client.OpJoinRandomOrCreateRoom(joinRoomParams, enterRoomParams);
        }

        private void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Console.WriteLine(arg1 + " -> " + arg2);
        }

        public void SendPositionUpdate(float x, float y, float z)
        {
            const byte eventCode = 1; // make up event codes at will, < 200
            var evData = new float[] { x, y, z };

            _client.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        // from IConnectionCallbacks:

        public void OnConnected()
        {
            Console.WriteLine("OnConnected");
        }

        public void OnConnectedToMaster()
        {
            Console.WriteLine("OnConnectedToMaster Server: " + _client.RealtimePeer.ServerIpAddress);
            MyJoinRandomOrCreateRoom();
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Console.WriteLine($"OnDisconnected: {cause}");
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Console.WriteLine("OnRegionListReceived");
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Console.WriteLine("OnCustomAuthenticationResponse");

            foreach (var kvp in data)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Console.WriteLine("OnCustomAuthenticationFailed: " + debugMessage);
        }
    }
}