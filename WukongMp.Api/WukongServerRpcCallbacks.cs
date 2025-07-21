using System;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using System;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public partial class WukongServerRpcCallbacks : IDisposable // TODO: Base class?
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;

    public WukongServerRpcCallbacks(
        RelaySerializer serializer,
        IRelayClient relayClient)
    {
        Serializer = serializer;
        RelayClient = relayClient;

        InitRpc();
    }

    public void Dispose()
    {
        DeInitRpc();
    }

    [ServerRpcEvent("SkipMovie")]
    internal void OnSkipMovie(int sequenceId)
    {
        Logging.LogDebug("Received skip movie event from server, sequence id: {Id}", sequenceId);
        InfoMessageWidget.Instance.SetVisibility(false);
        CutsceneUtils.SkipCutscene(sequenceId);
    }
}