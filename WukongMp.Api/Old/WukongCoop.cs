using System;
using System.Linq;
using CSharpModBase;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old;

public sealed class WukongCoop : IDisposable
{
    private readonly IRelayClient _relayClient;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongSynchronizer _synchronizer;
    private readonly WukongGameplaySettings _gameplaySettings;

    public WukongCoop(
        IRelayClient relayClient,
        WukongPlayerRegistry playerRegistry,
        WukongSynchronizer synchronizer,
        WukongGameplaySettings gameplaySettings
    )
    {
        _relayClient = relayClient;
        _playerRegistry = playerRegistry;
        _synchronizer = synchronizer;
        _gameplaySettings = gameplaySettings;

        _synchronizer.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
        _synchronizer.OnOtherPlayerJoined += SetMonsterScaling;
        _synchronizer.OnOtherPlayerLeft += SetMonsterScaling;
    }

    private void SetMonsterScaling(PlayerId _)
    {
        if (!_relayClient.IsMasterClient)
            return;

        var numPlayers = _playerRegistry.AllConnectedPlayers.Count();
        _gameplaySettings.SetMonsterHpScaling(numPlayers);
    }

    public void Dispose()
    {
        _synchronizer.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
        _synchronizer.OnOtherPlayerJoined -= SetMonsterScaling;
        _synchronizer.OnOtherPlayerLeft -= SetMonsterScaling;
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