using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Serialization;
using System;
using System.Diagnostics;
using b1;
using b1.EventDelDefine;
using HarmonyLib;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.Wukong.RPC;
using UnrealEngine.Engine;

namespace WukongMp.Api;

public partial class WukongServerRpcCallbacks : IDisposable // TODO: Base class?
{
    protected readonly IRelayClient RelayClient;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly ILogger _logger;
    private readonly WukongWidgetManager _widgetManager;

    public WukongServerRpcCallbacks(
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        ILogger logger,
        WukongWidgetManager widgetManager)
    {
        RelayClient = relayClient;
        _ecsLoop = ecsLoop;
        _logger = logger;
        _widgetManager = widgetManager;

        InitRpc();
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
            self._logger.LogDebug("Received skip movie event from server, sequence id: {Id}, waiting: {Waiting}/{All}", data0.SequenceId, data0.WaitingPlayers, data0.AllPlayers);

            if (data0.WaitingPlayers == data0.AllPlayers)
            {
                self._widgetManager.HideInfoMessage();
                CutsceneUtils.SkipCutscene(data0.SequenceId);
            }
            else
            {
                self._widgetManager.ShowInfoMessage(string.Format(Texts.WaitForOtherPlayersCount, data0.WaitingPlayers, data0.AllPlayers));
            }
        }, this, data);
    }

    [ServerRpcEvent("MovieStarted")]
    private void OnMovieStarted(int sequenceId, AreaId areaId)
    {
        // Do nothing on response from server.
    }

    [ServerRpcEvent("MovieFinished")]
    private void OnMovieFinished(int sequenceId, AreaId areaId)
    {
        // Do nothing on response from server.
    }

    [ServerRpcEvent("BeguilingChant")]
    private void OnBeguilingChant(byte rawState)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, state) =>
        {
            var areaActors = UGameplayStatics.GetAllActorsOfClass<BGUIntervalArea>(GameUtils.GetWorld());

            foreach (var area in areaActors)
            {
                var comp = area.GetComponent<BUS_IntervalTriggerImpl>();
                if (comp != null)
                {
                    var isActive = state == BeguilingChantState.Active;
                    var isWarnig = state == BeguilingChantState.Warning;
                    AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "SetIsActive").Invoke(comp, [isActive]);

                    if (isWarnig)
                    {
                        AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "CheckIsWarning").Invoke(comp, [0f]);
                    }
                    else
                    {
                        AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "ResetNotiedWarning").Invoke(comp, []);
                    }
                }
            }
        }, this, (BeguilingChantState)rawState);
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