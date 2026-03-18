using System;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client;

namespace WukongMp.Api;

internal class PingWidgetUpdater(NetworkPingMonitor pingMonitor, WukongServerRpcCallbacks rpc) : IScopedLifetime, IDisposable
{
    public void OnScopeStart()
    {
        pingMonitor.OnPingUpdated += HandlePingUpdated;
    }

    public void Dispose()
    {
        pingMonitor.OnPingUpdated -= HandlePingUpdated;
    }

    private void HandlePingUpdated(int _)
    {
        // we ignore LiteNetLib ping, instead showing the RPC ping that we send ourselves
        rpc.SendPing();
    }
}