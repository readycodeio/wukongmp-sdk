using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Serialization;
using System;
using System.Diagnostics;
using b1.EventDelDefine;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public partial class WukongServerRpcCallbacks : IDisposable // TODO: Base class?
{
    protected readonly IRelayClient RelayClient;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly ILogger _logger;

    public WukongServerRpcCallbacks(
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        ILogger logger)
    {
        RelayClient = relayClient;
        _ecsLoop = ecsLoop;
        _logger = logger;

        InitRpc();
    }

    public void Dispose()
    {
        DeInitRpc();
    }

    [ServerRpcEvent("SkipMovie")]
    private void OnSkipMovie(int sequenceId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sequenceId0) =>
        {
            self._logger.LogDebug("Received skip movie event from server, sequence id: {Id}", sequenceId0);
            InfoMessageWidget.Instance.SetVisibility(false);
            CutsceneUtils.SkipCutscene(sequenceId0);
        }, this, sequenceId);
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

            PingIndicatorWidget.Instance.SetPingValue(999);
            PingIndicatorWidget.Instance.SetInfoText(Texts.SeverePacketLossDetected);
            return;
        }

        var now = PingStopwatch.ElapsedMilliseconds;
        var rtt = now - timestamp;
        PingIndicatorWidget.Instance.SetPingValue(rtt);
        PingIndicatorWidget.Instance.HideInfoText();
    }

    public void SendPing()
    {
        _lastPingTimestamp = PingStopwatch.ElapsedMilliseconds;
        SendPing(_lastPingTimestamp);
    }
}