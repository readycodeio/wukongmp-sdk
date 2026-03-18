using System;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.Configuration;

namespace WukongMp.Api;

[Obsolete]
internal class WukongConnectionManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly RelayClientService _relayClientService;
    private readonly IRelayClient _relayClient;
    private readonly ClientState _state;

    public WukongConnectionManager(IRelayClient relayClient,
        ClientState state,
        ILogger logger)
    {
        _relayClient = relayClient;
        _state = state;
        _logger = logger;
        _relayClientService = new RelayClientService(relayClient, logger);

        state.OnConnected += OnConnectedHandler;
        state.OnDisconnected += OnDisconnectedHandler;
    }

    public bool IsRunning
        => _relayClientService.IsRunning;

    public bool RequestedConnect
        => _relayClient.RequestedConnect;

    private AreaId? RequestedAreaId
        => _relayClient.RequestedAreaId;

    private static void OnConnectedHandler(PlayerId player, Entity entity)
    {
        entity.GetComponent<PlayerComponent>().Nickname = LaunchParameters.Instance.Nickname;
    }

    public void Dispose()
    {
        _state.OnConnected -= OnConnectedHandler;
        _state.OnDisconnected -= OnDisconnectedHandler;
    }

    public void Start()
    {
        _relayClientService.Start();
    }

    public void Stop()
    {
        _relayClientService.Stop();
    }

    public void Connect()
    {
        _relayClient.RequestConnect();
    }

    public void Disconnect()
    {
        if (RequestedAreaId != null)
            LeaveArea();
        if (RequestedConnect)
            _relayClient.RequestDisconnect();
    }

    public void JoinArea(AreaId areaId)
    {
        _relayClient.RequestJoinArea(areaId);
    }

    public void LeaveArea()
    {
        _relayClient.RequestLeaveArea();
    }

    public void Reconnect()
    {
        Logging.LogInformation("Attempting to reconnect...");

        _relayClient.Scheduler.Schedule(async void (_, self) =>
        {
            try
            {
                var areaId = self.RequestedAreaId;

                if (self.RequestedAreaId != null)
                    self.LeaveArea();
                if (self.RequestedConnect)
                    self.Disconnect();

                await Task.Delay(Constants.ReconnectDelayMs);

                if (!self.RequestedConnect)
                    self.Connect();

                if (areaId.HasValue)
                {
                    await Task.Delay(Constants.ReconnectDelayMs);
                    self.JoinArea(areaId.Value);
                }
            }
            catch (Exception ex)
            {
                self._logger.LogError(ex, "Error while reconnecting");
            }
        }, this);
    }

    public void OnDisconnectedHandler(PlayerId playerId, Entity? entity, DisconnectReason disconnectReason)
    {
        Logging.LogInformation("Disconnected");
        if (disconnectReason == DisconnectReason.DisconnectPeerCalled)
        {
            Logging.LogInformation("Disconnected: {Cause}", disconnectReason);
        }
        else
        {
            Logging.LogWarning("Disconnected: {Cause}", disconnectReason);
            Reconnect();
        }
    }
}