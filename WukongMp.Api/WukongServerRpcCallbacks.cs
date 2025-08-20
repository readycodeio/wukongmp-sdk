using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Serialization;
using System;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public partial class WukongServerRpcCallbacks : IDisposable // TODO: Base class?
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly ILogger _logger;

    public WukongServerRpcCallbacks(
        RelaySerializer serializer,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        ILogger logger)
    {
        Serializer = serializer;
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
        _ecsLoop.Scheduler.Schedule((_, self, sequenceId0) =>
        {
            self._logger.LogDebug("Received skip movie event from server, sequence id: {Id}", sequenceId0);
            InfoMessageWidget.Instance.SetVisibility(false);
            CutsceneUtils.SkipCutscene(sequenceId0);
        }, this, sequenceId);
    }
}
