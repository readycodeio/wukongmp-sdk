using System;
using LiteNetLib;
using ReadyM.Api.Idents;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

public interface IWukongEventApi
{
    event Action? OnBeginPlayGameplayLevel;
    event Action? OnEndPlayGameplayLevel;
    event Action? OnLoadingScreenClose;
    event Action? OnLevelLoaded;
    event Action? OnExitLevel;
    event Action<AreaId>? OnJoinedArea;
    event Action<AreaId>? OnLeftArea;
    event Action<ReadyMainCharacter>? OnPlayerPawnSpawned;
    event Action<ReadyMainCharacter>? OnMainCharacterEntityInitialized;
    event Action<ReadyMainCharacter>? OnPlayerChangedTeam;
    event Action? OnLocalPlayerBeforeRebirth;
    event Action<PlayerId, AreaId>? OnOtherPlayerInsideArea;
    event Action<PlayerId, AreaId>? OnOtherPlayerOutsideArea;
    event Action<PlayerId>? OnConnected;
    event Action<PlayerId, DisconnectReason>? OnDisconnected;
}