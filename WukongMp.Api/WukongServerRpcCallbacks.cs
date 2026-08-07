using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Relay.Client.Utilities;
using ReadyM.Wukong.Common.Rpc;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.UI;

// ReSharper disable InconsistentNaming

namespace WukongMp.Api;

internal partial class WukongServerRpcCallbacks(
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
            self.SendSkipMovie(ev.SequenceId);
        }, this);
    }

    partial void OnSkipMovie(SkipMovieData data)
    {
        RunOnGameThread(() =>
        {
            MappedEvent.InvokeInGameIfApplicable(
                new SkipMovieEvent(
                    sequenceId: data.SequenceId,
                    waitingPlayers: data.WaitingPlayers,
                    allPlayers: data.AllPlayers
                ), default(EmptyContext)
            );
        });
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

            RunOnGameThread(widgetManager.SetPacketLossWarning);
            return;
        }

        var now = PingStopwatch.ElapsedMilliseconds;
        var rtt = now - timestamp;
        sessionStats.AddPing(rtt);

        RunOnGameThread(() => { widgetManager.UpdatePingIndicator(rtt); });
    }

    public void SendPing()
    {
        _lastPingTimestamp = PingStopwatch.ElapsedMilliseconds;
        SendPing(_lastPingTimestamp);
    }
}