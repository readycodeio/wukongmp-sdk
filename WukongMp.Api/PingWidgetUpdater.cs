using System;
using ReadyM.Api.Multiplayer;
using WukongMp.Api.UI;

namespace WukongMp.Api;

public class PingWidgetUpdater : IDisposable
{
    private readonly NetworkPingMonitor _pingMonitor;
    
    public PingWidgetUpdater(NetworkPingMonitor pingMonitor)
    {
        _pingMonitor = pingMonitor;
        _pingMonitor.OnPingUpdated += HandlePingUpdated;
    }

    public void Dispose()
    {
        _pingMonitor.OnPingUpdated -= HandlePingUpdated;
    }
    
    private void HandlePingUpdated(int ping)
    {
        PingIndicatorWidget.Instance.SetPingValue(ping);
    }
}