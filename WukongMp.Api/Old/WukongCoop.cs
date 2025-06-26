using System;
using ReadyM.Relay.Client;

namespace WukongMp.Api.Old;

public class WukongCoop : IDisposable
{
    private readonly RelayClient _relayClient;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongPlayerPropertyManager _playerProperty;
    private readonly WukongSynchronizer _synchronizer;

    public WukongCoop(
        RelayClient relayClient,
        WukongPlayerRegistry playerRegistry,
        WukongPlayerPropertyManager playerProperty,
        WukongSynchronizer synchronizer
    )
    {
        _relayClient = relayClient;
        _playerRegistry = playerRegistry;
        _playerProperty = playerProperty;
        _synchronizer = synchronizer;
    }

    public void Dispose()
    {
        // empty
    }
}