using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LiteNetLib;
using ReadyM.Api.Idents;
using UnrealEngine.Engine;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

public interface IWukongClientApi
{
    void GetDisconnectReasonAndInvoke(Action<DisconnectReason> callback);
    bool InRoom { get; }
    bool IsConnected { get; }
    bool IsMasterClient { get; }
    PlayerId? LocalPlayerId { get; }
    AreaId? CurrentAreaId { get; }
    ReadyMainCharacter? LocalMainCharacter { get; }
    IReadOnlyList<PlayerId> AreaPlayers { get; }
    EntityList<ReadyTamer> AllTamers { get; }
    EntityList<ReadyMainCharacter> AllMainCharacters { get; }
    ReadyMainCharacter? GetPlayerEntityByActor(AActor actor);
    bool TryGetPlayerInfoById(PlayerId player, [NotNullWhen(true)] out string? nickname, [NotNullWhen(true)] out int? team);
    void SyncMonstersInArea();
}