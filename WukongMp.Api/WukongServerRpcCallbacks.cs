using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Relay.Client.Utilities;
using ReadyM.Wukong.Common.Rpc;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.UI;

// ReSharper disable InconsistentNaming

namespace WukongMp.Api;

internal partial class WukongServerRpcCallbacks(
    ReceiveSystem schedulerSystem,
    IMappedEventManager mappedEvent,
    NetworkSessionStats sessionStats,
    WukongWidgetManager widgetManager,
    ILogger logger
) : ServerRpcClient
{
    private IMappedEventManager MappedEvent => mappedEvent;

    public override void OnScopeStart()
    {
        base.OnScopeStart();

        MappedEvent.RegisterEcsEventHandler<SkipMovieEvent, WukongServerRpcCallbacks>(static (ev, self) =>
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

    partial void OnSkipMovie(SkipMovieData data)
    {
        schedulerSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            self.MappedEvent.InvokeInGameIfApplicable(
                new SkipMovieEvent(
                    sequenceId: data0.SequenceId,
                    waitingPlayers: data0.WaitingPlayers,
                    allPlayers: data0.AllPlayers
                ), default(EmptyContext)
            );
        }, this, data);
    }

    private static readonly Stopwatch PingStopwatch = Stopwatch.StartNew();
    private static long _lastPingTimestamp;

    partial void OnPing(long timestamp)
    {
        if (timestamp != _lastPingTimestamp)
        {
            // Outdated ping response, most likely due to packet loss
            var outdatedRtt = PingStopwatch.ElapsedMilliseconds - timestamp;
            logger.LogWarning("Received outdated ping response. Timestamp: {Timestamp}, now: {Now}, RTT: {Rtt}ms", timestamp, PingStopwatch.ElapsedMilliseconds, outdatedRtt);

            widgetManager.SetPacketLossWarning();
            return;
        }

        var now = PingStopwatch.ElapsedMilliseconds;
        var rtt = now - timestamp;
        widgetManager.UpdatePingIndicator(rtt);
        sessionStats.AddPing(rtt);
    }

    public void SendPing()
    {
        _lastPingTimestamp = PingStopwatch.ElapsedMilliseconds;
        SendPing(_lastPingTimestamp);
    }
}