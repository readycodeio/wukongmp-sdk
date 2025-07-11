using System;
using CSharpModBase;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

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

        _synchronizer.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
    }

    public void Dispose()
    {
        _synchronizer.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
    }
    
    private void OnAfterJoinedRoomHandler()
    {
        if (RelayClient.IsMasterClient)
        {
            Utils.TryRunOnGameThread(TamerUtils.DiscoverTamers);
        }
        
        CoopStatusWidget.Instance.SetVisibility(true);
    }
}