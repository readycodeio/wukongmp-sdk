using System;
using System.Collections.Generic;
using System.Numerics;
using LiteNetLib;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
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
using WukongMp.Api.State;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

public class WukongClientApi
{
    private readonly ClientWukongArchetypeRegistration wukongArchetype;
    private readonly Store world;
    private readonly ClientState state;
    private readonly WukongAreaState areaState;
    private readonly WukongPlayerState playerState;
    private readonly WukongPawnState pawnState;
    private readonly IEntityManager entityManager;
    private readonly IRelayClient relayClient;
    private readonly WukongSaveRelay saveRelay;
    private readonly MappedEventManager mappedEvent;

    internal WukongClientApi(ClientWukongArchetypeRegistration wukongArchetype,
        Store world,
        ClientState state,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongPawnState pawnState,
        IEntityManager entityManager,
        IRelayClient relayClient,
        WukongSaveRelay saveRelay,
        MappedEventManager mappedEvent)
    {
        this.wukongArchetype = wukongArchetype;
        this.world = world;
        this.state = state;
        this.areaState = areaState;
        this.playerState = playerState;
        this.pawnState = pawnState;
        this.entityManager = entityManager;
        this.relayClient = relayClient;
        this.saveRelay = saveRelay;
        this.mappedEvent = mappedEvent;
    }

    internal MappedEventManager MappedEvent
        => mappedEvent;

    // ---
    
    public void GetDisconnectReasonAndInvoke(Action<DisconnectReason> callback)
    {
        relayClient.Scheduler.Schedule((ctx, call) =>
        {
            call(ctx.LastDisconnectReason);
        }, callback);
    }
    
    public IWukongSaveRelay Saves => saveRelay;

    public bool InRoom
        => areaState.InRoom;

    public bool IsConnected
        => state.IsConnected;

    public PlayerId? LocalPlayerId
        => state.LocalPlayerId;

    public PlayerId? MasterClientId
        => areaState.MasterClientId;

    public bool IsMasterClient
        => areaState.IsMasterClient;

    public AreaId? CurrentAreaId
        => state.CurrentAreaId;

    public ReadyMainCharacter? LocalMainCharacter
        => playerState.LocalMainCharacter != null ? new ReadyMainCharacter(this, playerState.LocalMainCharacter.Value.Entity) : null;

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

    public ReadyMainCharacter CreateMainCharacter(Vector3 location, Vector3 rotation, int teamId)
    {
        var entity = entityManager.CreateAreaEntity(wukongArchetype.MainCharacterArchetype, b =>
        {
            b.Add(new TransformComponent
            {
                Position = location,
                Rotation = rotation
            });
            b.Add(new TeamComponent
            {
                TeamId = teamId
            });
        });
        return new ReadyMainCharacter(this, entity);
    }

    public ReadyTamer CreateTamer(Vector3 location, Vector3 rotation, TamerKind tamerKind, int teamId)
    {
        var entity = entityManager.CreateAreaEntity(wukongArchetype.TamerArchetype, b =>
        {
            var guid = Guid.NewGuid();
            var unitPath = UnitPathUtils.GetUnitPathName(tamerKind);
            b.Add(new TamerComponent()
            {
                Guid = guid.ToString(),
                UnitPath = unitPath,
            });
            b.Add(new TeamComponent
            {
                TeamId = teamId
            });
            b.Add(new TransformComponent
            {
                Position = location,
                Rotation = rotation
            });
        });
        return new ReadyTamer(this, entity);
    }

    public void DestroyObject(ReadyObject obj)
    {
        var entity = obj.Entity;
        entity.DeleteEntity();
    }

    public ReadyMainCharacter? GetEntityByPlayerActor(AActor? actor)
    {
        var entity = pawnState.GetEntityByPlayerActor(actor);
        if (entity != null)
        {
            return new ReadyMainCharacter(this, entity.Value.Entity);
        }

        return null;
    }
}