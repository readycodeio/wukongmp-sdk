using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LiteNetLib;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

/// API for networked gameplay features.
public sealed class WukongClientApi
{
    private readonly Store world;
    private readonly ClientState state;
    private readonly WukongAreaState areaState;
    private readonly WukongPlayerState playerState;
    private readonly IRelayClient relayClient;

    internal WukongClientApi(Store world,
        ClientState state,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        IRelayClient relayClient)
    {
        this.world = world;
        this.state = state;
        this.areaState = areaState;
        this.playerState = playerState;
        this.relayClient = relayClient;
    }

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

    public bool TryGetPlayerById(PlayerId player, [NotNullWhen(true)] out ReadyMainCharacter? mainCharacter)
    {
        var entity = playerState.GetMainCharacterByPlayerId(player);
        if (entity.HasValue)
        {
            mainCharacter = new ReadyMainCharacter(this, entity.Value);
            return true;
        }

        mainCharacter = null;
        return false;
    }

    public void SyncMonstersInArea()
    {
        TamerUtils.DiscoverTamers();
    }

    // public ReadyMainCharacter CreateMainCharacter(Vector3 location, Vector3 rotation, int teamId)
    // {
    //     var entity = entityManager.CreateAreaEntity(wukongArchetype.MainCharacterArchetype, b =>
    //     {
    //         b.Add(new TransformComponent
    //         {
    //             Position = location,
    //             Rotation = rotation
    //         });
    //         b.Add(new TeamComponent
    //         {
    //             TeamId = teamId
    //         });
    //     });
    //     return new ReadyMainCharacter(this, entity);
    // }
    //
    // public ReadyTamer CreateTamer(Vector3 location, Vector3 rotation, TamerKind tamerKind, int teamId)
    // {
    //     var entity = entityManager.CreateAreaEntity(wukongArchetype.TamerArchetype, b =>
    //     {
    //         var guid = Guid.NewGuid();
    //         var unitPath = UnitPathUtils.GetUnitPathName(tamerKind);
    //         b.Add(new TamerComponent()
    //         {
    //             Guid = guid.ToString(),
    //             UnitPath = unitPath,
    //         });
    //         b.Add(new TeamComponent
    //         {
    //             TeamId = teamId
    //         });
    //         b.Add(new TransformComponent
    //         {
    //             Position = location,
    //             Rotation = rotation
    //         });
    //     });
    //     return new ReadyTamer(this, entity);

    // }
}