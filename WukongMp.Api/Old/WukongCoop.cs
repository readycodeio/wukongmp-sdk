using System;
using CSharpModBase;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Common;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old;

public class WukongCoop : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly WukongRoomState _roomState;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongSynchronizer _synchronizer;

    public WukongCoop(
        RelaySerializer serializer,
        IRelayClient relayClient,
        WukongRoomState roomState,
        WukongPlayerRegistry playerRegistry,
        WukongSynchronizer synchronizer
    )
    {
        Serializer = serializer; 
        RelayClient = relayClient;
        _roomState = roomState;
        _playerRegistry = playerRegistry;
        _synchronizer = synchronizer;

        _synchronizer.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
    }

    public void Dispose()
    {
        _synchronizer.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
    }
    
    private void OnAfterJoinedRoomHandler()
    {
        if (_roomState.IsMasterClient)
        {
            Utils.TryRunOnGameThread(TamerUtils.DiscoverTamers);
        }
        
        CoopStatusWidget.Instance.SetVisibility(true);
    }
}