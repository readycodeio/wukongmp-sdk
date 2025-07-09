using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using b1;
using BtlShare;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old.State;

namespace WukongMp.Api.Old;

public class WukongPlayerPropertyManager(IRelayClient relayClient, WukongPlayerRegistry playerRegistry)
{
    private readonly object _playerPropertiesLock = new();
    private ConcurrentDictionary<string, object> _playerProperties = new();
    private ConcurrentDictionary<string, object> _playerPropertiesRo = new();

    [Obsolete]
    public void SetCachedPlayerProperties()
    {
        lock (_playerPropertiesLock)
        {
            (_playerProperties, _playerPropertiesRo) = (_playerPropertiesRo, _playerProperties);

            if (_playerPropertiesRo.Count == 0)
                return;

            var hashtable = new Dictionary<object, object?>();
            foreach (var (key, value) in _playerPropertiesRo)
            {
                hashtable[key] = value;
            }

            _playerPropertiesRo.Clear();
            relayClient.OpSetCustomPropertiesOfActor(relayClient.PlayerId, hashtable);
        }
    }
    
    public void CachePlayerProperty(string key, object value)
    {
        _playerProperties[key] = value;
        if (!(value is FVector || value is FRotator || key == nameof(PlayerState.TurnInplaceRemainAngle)))
        {
            Logging.LogTrace("Set player property: {Property} = {Value}", key, value);
        }
    }

    public void CachePlayerAttribute(EBGUAttrFloat attr, float value)
    {
        // if HpMax changed, update Hp too
        if (relayClient.IsMasterClient && playerRegistry.HasLocalPlayerState && attr == EBGUAttrFloat.HpMaxBase)
        {
            var data = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(playerRegistry.LocalPlayerState.Pawn);
            var currentHp = data.GetFloatValue(EBGUAttrFloat.Hp);

            playerRegistry.LocalPlayerState.Hp = currentHp;
            CachePlayerProperty(nameof(PlayerState.Hp), currentHp);
        }

        CachePlayerProperty($"{Constants.AttributePrefix}{attr}", value);
    }

    public void SetRemotePlayerProperty(PlayerId playerId, string key, object value)
    {
        if (!relayClient.IsMasterClient)
        {
            Logging.LogError("Only room owner can send remote player properties.");
            return;
        }

        var hashtable = new Dictionary<object, object?>
        {
            [key] = value
        };

        Logging.LogDebug("Sending remote player property: {Property} = {Value}", key, value);

        relayClient.OpSetCustomPropertiesOfActor(playerId, hashtable);
    }
}