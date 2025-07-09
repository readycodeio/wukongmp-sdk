using System;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;

namespace WukongMp.Api.Old;

public class WukongCoop : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongPlayerPropertyManager _playerProperty;
    private readonly WukongSynchronizer _synchronizer;

    public WukongCoop(
        RelaySerializer serializer,
        IRelayClient relayClient,
        WukongPlayerRegistry playerRegistry,
        WukongPlayerPropertyManager playerProperty,
        WukongSynchronizer synchronizer
    )
    {
        Serializer = serializer; 
        RelayClient = relayClient;
        _playerRegistry = playerRegistry;
        _playerProperty = playerProperty;
        _synchronizer = synchronizer;
    }

    public void Dispose()
    {
        // empty
    }
}