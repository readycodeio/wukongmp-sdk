using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Protocol.Enums;
using System;

namespace WukongMp.Api;

public partial class WukongServerRpcCallbacks : IDisposable
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

    [ServerRpcEvent(ServerRpcCode.DataMessage)]
    internal void OnDataMessage(int value)
    {
        Logging.LogDebug("Received data from server RPC, value: {Value}", value);
    }
}
