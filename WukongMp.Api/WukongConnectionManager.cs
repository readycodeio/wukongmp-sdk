using System;
using System.Linq;
using System.Threading.Tasks;
using LiteNetLib;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Old.State;

namespace WukongMp.Api;

public class WukongConnectionManager : IDisposable
{
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly RoomStateProxy _roomState;
    private readonly NetworkedStateSynchronizer _synchronizer;

    public IRelayClient RelayClient { get; }
    
    public bool IsRunning { get; private set; }
    public bool EnteredRoom { get; private set; }
    
    public event Action<string>? OnMasterClientChanged;

    public WukongConnectionManager(IRelayClient relayClient,
        WukongPlayerRegistry playerRegistry,
        NetworkedStateSynchronizer synchronizer,
        RoomStateProxy roomState)
    {
        _playerRegistry = playerRegistry;
        _roomState = roomState;
        _synchronizer = synchronizer;
        RelayClient = relayClient;
        
        RelayClient.OnDisconnected += OnDisconnectedHandler;
    }

    public void Dispose()
    {
        Stop();
        RelayClient.OnDisconnected -= OnDisconnectedHandler;
    }

    public void Start()
    {
        if (IsRunning)
            return;
        IsRunning = true;
        
        _synchronizer.Start();
        RelayClient.Start();
    }

    public void Stop()
    {
        if (!IsRunning)
            return;
        IsRunning = false;

        if (EnteredRoom)
            ExitRoom();
        RelayClient.Stop();
        _synchronizer.Stop();
    }

    public void EnterRoom()
    {
        if (EnteredRoom)
            return;
        EnteredRoom = true;
        
        RelayClient.EnterRoom();
    }

    public void ExitRoom()
    {
        if (!EnteredRoom)
            return;
        EnteredRoom = false;
 
        RelayClient.ExitRoom();
    }
    
    public void Reconnect()
    {
        if (!IsRunning)
            return;
        
        Logging.LogInformation("Attempting to reconnect...");
        _ = Task.Run(async () =>
        {
            if (EnteredRoom)
                RelayClient.ExitRoom();
            if (IsRunning)
                RelayClient.Stop();
            await Task.Delay(Constants.ReconnectDelayMs);
            if (IsRunning)
                RelayClient.Start();
            if (EnteredRoom)
                RelayClient.EnterRoom();
        });
    }
    
    public void Disconnect()
    {
        if (EnteredRoom)
            RelayClient.ExitRoom();
        if (IsRunning)
            RelayClient.Stop();
    }
    
    public void SetMasterClient(string newMasterName)
    {
        if (RelayClient.IsMasterClient)
        {
            var newMasterPlayer = _playerRegistry.AllConnectedPlayers.FirstOrDefault(x => x.NickName == newMasterName);
            if (newMasterPlayer != null)
            {
                _roomState.MasterClientId = newMasterPlayer.PlayerId;
                OnMasterClientChanged?.Invoke(newMasterName);
            }
            else
            {
                Logging.LogError("Player {PlayerName} not found", newMasterName);
            }
        }
    }
    
    public void OnDisconnectedHandler(DisconnectReason reason)
    {
        Logging.LogInformation("Disconnected");
        if (reason == DisconnectReason.DisconnectPeerCalled)
        {
            Logging.LogInformation("Disconnected: {Cause}", reason);
        }
        else
        {
            Logging.LogWarning("Disconnected: {Cause}", reason);
        }

        if (reason is DisconnectReason.Timeout or DisconnectReason.RemoteConnectionClose)
        {
            Reconnect();
        }
    }
}
