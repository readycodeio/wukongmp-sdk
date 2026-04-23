using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using b1;
using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

public interface IWukongSynchronizationApi
{
    void GetDisconnectReasonAndInvoke(Action<DisconnectReason> callback);
    bool InRoom { get; }
    bool IsConnected { get; }
    bool IsMasterClient { get; }
    PlayerId? LocalPlayerId { get; }
    AreaId? CurrentAreaId { get; }
    ReadyMainCharacter? LocalMainCharacter { get; }
    IReadOnlyList<PlayerId> AllPlayers { get; }
    IReadOnlyList<PlayerId> AreaPlayers { get; }
    EntityList<ReadyTamer> AllTamers { get; }
    EntityList<ReadyTamer> AreaTamers { get; }
    EntityList<ReadyMainCharacter> AllMainCharacters { get; }
    EntityList<ReadyMainCharacter> AreaMainCharacters { get; }
    ReadyMainCharacter? GetPlayerEntityByActor(AActor? actor);
    ReadyMainCharacter? GetPlayerEntityByLastTransformation(BGUCharacterCS? targetCharacter);
    bool TryGetPlayerInfoById(PlayerId player, [NotNullWhen(true)] out string? nickname, [NotNullWhen(true)] out int? team);
    ReadyMainCharacter? GetMainCharacterByPlayerId(PlayerId playerId);
    void SyncMonstersInArea();
    void SpawnEnemy(TamerKind kind, Vector3 position, int count = 1, int teamId = Constants.DefaultMonsterTeamId);
    void EnableSpectatorMode(ReadyMainCharacter character, SpectatorReason reason);
    void DisableSpectatorMode(ReadyMainCharacter character);
}