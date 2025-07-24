using System;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;

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

    [ServerRpcEvent("ExampleEvent")]
    internal void OnExampleEvent(int value)
    {
        Logging.LogDebug("Received data from server RPC, value: {Value}", value);
    }
}