using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Mapping;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong.DTO;
using ReadyM.Relay.Common.Wukong.ECS.Values;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Mapping;
using WukongMp.Api.UI;

// ReSharper disable InconsistentNaming

namespace WukongMp.Api;

public partial class WukongServerRpcCallbacks : IDisposable // TODO: Base class?
{
    protected readonly IRelayClient RelayClient;
    protected readonly RelaySerializer Serializer;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly MappedEventManager _mappedEvent;
    private readonly WukongMappingPolicyDirectory _policyDir;
    private readonly WukongWidgetManager _widgetManager;
    private readonly ILogger _logger;

    public WukongServerRpcCallbacks(
        IClientEcsUpdateLoop ecsLoop,
        MappedEventManager mappedEvent,
        WukongMappingPolicyDirectory policyDir,
        RelaySerializer serializer,
        IRelayClient relayClient,
        WukongWidgetManager widgetManager,
        ILogger logger)
    {
        RelayClient = relayClient;
        Serializer = serializer;
        _ecsLoop = ecsLoop;
        _mappedEvent = mappedEvent;
        _policyDir = policyDir;
        _widgetManager = widgetManager;
        _logger = logger;

        InitRpc();
        
        _mappedEvent.RegisterGameEventHandler<SkipMovieEvent, WukongServerRpcCallbacks>(static (ev, self) =>
        {
            self.SendSkipMovie(
                new SkipMovieData(
                    sequenceId: ev.SequenceId,
                    waitingPlayers: ev.WaitingPlayers,
                    allPlayers: ev.AllPlayers
                )
            );
        }, this);
    }

    public void Dispose()
    {
        DeInitRpc();
    }

    [ServerRpcEvent("SkipMovie")]
    private void OnSkipMovie(SkipMovieData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._policyDir.ForEvent<SkipMovieEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(
                    new SkipMovieEvent(
                        sequenceId: data0.SequenceId,
                        waitingPlayers: data0.WaitingPlayers,
                        allPlayers: data0.AllPlayers
                    )
                );
            }
        }, this, data);
    }

    // NOTE: This is declared here in order to generate send methods
    [ServerRpcEvent("MovieStarted")]
    private void OnMovieStarted(int sequenceId, AreaId areaId)
    {
        // Do nothing on response from server.
    }

    // NOTE: This is declared here in order to generate send methods
    [ServerRpcEvent("MovieFinished")]
    private void OnMovieFinished(int sequenceId, AreaId areaId)
    {
        // Do nothing on response from server.
    }

    [ServerRpcEvent("BeguilingChant")]
    private void OnBeguilingChant(byte stateRaw)
    {
        var state = (BeguilingChantState)stateRaw;
        _ecsLoop.Scheduler.Schedule(static (_, self, state0) =>
        {
            if (self._policyDir.ForEvent<BeguilingChantEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new BeguilingChantEvent(
                    state: state0
                ));
            }
        }, this, state);
    }

    [ServerRpcEvent("EnableCheats")]
    private void OnEnableCheats(AreaId areaId, bool enabled)
    {
        // Do nothing on response from server.
    }

    private static readonly Stopwatch PingStopwatch = Stopwatch.StartNew();
    private static long _lastPingTimestamp;

    [ServerRpcEvent("Ping")]
    private void OnPing(long timestamp)
    {
        if (timestamp != _lastPingTimestamp)
        {
            // Outdated ping response, most likely due to packet loss
            var outdatedRtt = PingStopwatch.ElapsedMilliseconds - timestamp;
            _logger.LogWarning("Received outdated ping response. Timestamp: {Timestamp}, now: {Now}, RTT: {Rtt}ms", timestamp, PingStopwatch.ElapsedMilliseconds, outdatedRtt);

            _widgetManager.SetPacketLossWarning();
            return;
        }

        var now = PingStopwatch.ElapsedMilliseconds;
        var rtt = now - timestamp;
        _widgetManager.UpdatePingIndicator(rtt);
    }

    public void SendPing()
    {
        _lastPingTimestamp = PingStopwatch.ElapsedMilliseconds;
        SendPing(_lastPingTimestamp);
    }
}