using System;
using ReadyM.Api.Multiplayer.Client;

namespace WukongMp.Api;

internal class PingWidgetUpdater : IDisposable
{
    private readonly NetworkPingMonitor _pingMonitor;
    private readonly WukongServerRpcCallbacks _rpc;
    
    public PingWidgetUpdater(NetworkPingMonitor pingMonitor, WukongServerRpcCallbacks rpc)
    {
        _rpc = rpc;
        _pingMonitor = pingMonitor;
        _pingMonitor.OnPingUpdated += HandlePingUpdated;
    }

    public void Dispose()
    {
        _pingMonitor.OnPingUpdated -= HandlePingUpdated;
    }
    
    private void HandlePingUpdated(int _)
    {
        // we ignore LiteNetLib ping, instead showing the RPC ping that we send ourselves
        _rpc.SendPing();
    }
}