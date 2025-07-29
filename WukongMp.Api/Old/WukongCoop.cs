using System;
using CSharpModBase;
using ReadyM.Relay.Client;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old;

public sealed class WukongCoop : IDisposable
{
    private readonly IRelayClient _relayClient;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongSynchronizer _synchronizer;

    public WukongCoop(
        IRelayClient relayClient,
        WukongPlayerRegistry playerRegistry,
        WukongSynchronizer synchronizer
    )
    {
        _relayClient = relayClient;
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
        if (_relayClient.IsMasterClient)
        {
            Utils.TryRunOnGameThread(TamerUtils.DiscoverTamers);
        }

        CoopStatusWidget.Instance.SetVisibility(true);
    }
}