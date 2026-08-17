using System;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using Yooni.Native.Container;
using Constants = WukongMp.Api.Configuration.Constants;

namespace WukongMp.Api;

[Obsolete]
internal class WukongConnectionManager(
    IRelayClient relayClient,
    ClientState state,
    ILogger logger
) : IHostedService
{
    private readonly ILogger _logger = logger;
    private readonly RelayClientService _relayClientService = new(relayClient, logger);

    public void OnScopeStart()
    {
        state.OnConnected += OnConnectedHandler;
        state.OnDisconnected += OnDisconnectedHandler;
    }

    public void Dispose()
    {
        state.OnConnected -= OnConnectedHandler;
        state.OnDisconnected -= OnDisconnectedHandler;
    }

    public bool IsRunning
        => _relayClientService.IsRunning;

    public bool RequestedConnect
        => relayClient.RequestedConnect;

    private AreaId? RequestedAreaId
        => relayClient.RequestedAreaId;

    private static void OnConnectedHandler(PlayerId player, Entity entity)
    {
        entity.GetComponent<PlayerComponent>().Nickname = new NativeString256(LaunchParameters.Instance.Nickname, false);
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
        relayClient.RequestConnect();
    }

    public void Disconnect()
    {
        if (RequestedAreaId != null)
            LeaveArea();
        if (RequestedConnect)
            relayClient.RequestDisconnect();
    }

    public void JoinArea(AreaId areaId)
    {
        relayClient.RequestJoinArea(areaId);
    }

    public void LeaveArea()
    {
        relayClient.RequestLeaveArea();
    }

    public void Reconnect()
    {
        Logging.LogInformation("Attempting to reconnect...");

        relayClient.Scheduler.Schedule(async void (_, self) =>
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

    private void OnDisconnectedHandler(PlayerId playerId, Entity? entity, DisconnectedReason disconnectReason)
    {
        Logging.LogInformation("Disconnected");
        if (disconnectReason == DisconnectedReason.ClientDisconnected)
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