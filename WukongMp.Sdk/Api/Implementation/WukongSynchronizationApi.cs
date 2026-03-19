using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Api.State;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api.Implementation;

/// API for networked gameplay features.
internal sealed class WukongSynchronizationApi(
    Store world,
    ClientState state,
    WukongAreaState areaState,
    WukongPlayerState playerState,
    WukongMappingPolicyDirectory mappingDir,
    IMappedEventManager mappedEvent,
    IRelayClient relayClient
) : IWukongSynchronizationApi
{
    // ---

    public void GetDisconnectReasonAndInvoke(Action<DisconnectReason> callback)
    {
        relayClient.Scheduler.Schedule((ctx, call) => { call(ctx.LastDisconnectReason); }, callback);
    }

    public bool InRoom
        => areaState.InRoom;

    public bool IsConnected
        => state.IsConnected;

    public bool IsMasterClient
        => areaState.IsMasterClient;

    public PlayerId? LocalPlayerId
        => state.LocalPlayerId;

    public AreaId? CurrentAreaId
        => state.CurrentAreaId;

    public ReadyMainCharacter? LocalMainCharacter
        => playerState.LocalMainCharacter != null ? new ReadyMainCharacter(this, playerState.LocalMainCharacter.Value) : null;

    public IReadOnlyList<PlayerId> AreaPlayers
        => state.AreaPlayers;

    public EntityList<ReadyTamer> AllTamers
    {
        get
        {
            var entityList = world.Query<TamerComponent>().ToEntityList();
            return new EntityList<ReadyTamer>(this, entityList);
        }
    }

    public EntityList<ReadyMainCharacter> AllMainCharacters
    {
        get
        {
            var entityList = world.Query<MainCharacterComponent>().ToEntityList();
            return new EntityList<ReadyMainCharacter>(this, entityList);
        }
    }

    public ReadyMainCharacter? GetPlayerEntityByActor(AActor actor)
    {
        if (mappingDir.IsMainCharacterMapped(actor, out var entity))
        {
            return new ReadyMainCharacter(this, entity.Value.Entity);
        }

        return null;
    }

    public bool TryGetPlayerInfoById(PlayerId player, [NotNullWhen(true)] out string? nickname, [NotNullWhen(true)] out int? team)
    {
        var entity = playerState.GetPlayerById(player);
        if (entity.HasValue)
        {
            var comp = entity.Value.GetState();
            nickname = comp.Nickname;
            team = comp.TeamId;
            return true;
        }

        nickname = null;
        team = null;
        return false;
    }

    public void SyncMonstersInArea()
    {
        TamerUtils.DiscoverTamers();
    }

    public void SpawnEnemy(TamerKind kind, Vector3 position)
    {
        if (LocalMainCharacter.HasValue && kind.Name != null)
        {
            mappedEvent.InvokeInGameAndNotifyEcs(new RequestSpawnUnitsEvent(LocalMainCharacter.Value.Entity, kind.Name, 1, 2, position.ToFVector()), LocalMainCharacter.Value.Entity.Entity);
        }
    }
}