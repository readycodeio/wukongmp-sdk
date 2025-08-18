using System;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.Old;
using WukongMp.Api.State;

namespace WukongMp.Api;

[Obsolete]
public class WukongConnectionManager : IDisposable
{
    public RelayClientService RelayClientService { get; }
    public IRelayClient RelayClient { get; }
    public WukongAreaState AreaState { get; }
    public WukongPlayerState PlayerState { get; }

    private readonly ClientState _state;
    private readonly ILogger _logger;

    public PlayerId? PlayerId => PlayerState.LocalPlayerId;

    public bool IsRunning
        => RelayClientService.IsRunning;

    public bool RequestedConnect
        => RelayClient.RequestedConnect;

    public AreaId? RequestedAreaId
        => RelayClient.RequestedAreaId;

    public event Action<string>? OnMasterClientChanged;

    public WukongConnectionManager(RelayClientService relayClientService,
        ClientState state,
        WukongPlayerState playerState,
        WukongAreaState areaState,
        ILogger logger)
    {
        RelayClientService = relayClientService;
        RelayClient = relayClientService.RelayClient;
        AreaState = areaState;
        PlayerState = playerState;
        _state = state;
        _logger = logger;

        _state.OnConnected += OnConnectedHandler;
        _state.OnDisconnected += OnDisconnectedHandler;
    }

    private static void OnConnectedHandler(PlayerId player, Entity entity)
    {
        entity.GetComponent<PlayerComponent>().NickName = CmdLineParams.Instance.Nickname;
    }

    public void Dispose()
    {
        _state.OnConnected -= OnConnectedHandler;
        _state.OnDisconnected -= OnDisconnectedHandler;
    }

    public void Start()
    {
        RelayClientService.Start();
    }

    public void Stop()
    {
        RelayClientService.Stop();
    }

    public void Connect()
    {
        RelayClient.RequestConnect();
    }

    public void Disconnect()
    {
        if (RequestedAreaId != null)
            LeaveArea();
        if (RequestedConnect)
            RelayClient.RequestDisconnect();
    }

    public void JoinArea(AreaId areaId)
    {
        RelayClient.RequestJoinArea(Constants.MainArea);
    }

    public void LeaveArea()
    {
        RelayClient.RequestLeaveArea();
    }

    public void Reconnect()
    {
        Logging.LogInformation("Attempting to reconnect...");

        RelayClient.Scheduler.Schedule(async void (context, self) =>
        {
            try
            {
                var requestedAreaId = self.RequestedAreaId;
                var requestedConnect = self.RequestedConnect;
                if (requestedAreaId != null)
                    self.LeaveArea();
                if (requestedConnect)
                    self.Disconnect();
                await Task.Delay(Constants.ReconnectDelayMs);
                if (!requestedConnect)
                    self.Connect();
                if (requestedAreaId != null)
                    self.JoinArea(requestedAreaId.Value);
            }
            catch (Exception ex)
            {
                self._logger.LogError(ex, "Error while reconnecting");
            }
        }, this);
    }

    public void SetMasterClient(string newMasterName)
    {
        if (AreaState.IsMasterClient)
        {
            var newMasterPlayerId = _state.AllPlayers.FirstOrDefault(x => PlayerState.GetPlayerById(x)?.GetState().NickName == newMasterName);
            if (newMasterPlayerId != null)
            {
                var areaEntity = AreaState.CurrentArea;
                if (areaEntity != null)
                {
                    areaEntity.Value.GetRoom().MasterClient = newMasterPlayerId;
                }

                // FIXME: We should send this when master client actually changes, not when we request it changing
                OnMasterClientChanged?.Invoke(newMasterName);
            }
            else
            {
                Logging.LogError("Player {PlayerName} not found", newMasterName);
            }
        }
    }

    public void OnDisconnectedHandler(PlayerId playerId, Entity entity, DisconnectReason disconnectReason)
    {
        Logging.LogInformation("Disconnected");
        if (disconnectReason == DisconnectReason.DisconnectPeerCalled)
        {
            Logging.LogInformation("Disconnected: {Cause}", disconnectReason);
        }
        else
        {
            Logging.LogWarning("Disconnected: {Cause}", disconnectReason);
        }

        // FIXME: This will only try to reconnect once and immediately which will probably not work if the cause 
        // is a weak network connection.
        if (disconnectReason is DisconnectReason.Timeout or DisconnectReason.RemoteConnectionClose)
        {
            Reconnect();
        }
    }
}