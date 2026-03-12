using System;
using System.Numerics;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk.Entities;

public readonly struct ReadyMainCharacter
    : IReadyEntity<ReadyMainCharacter>,
        IReadyConvertable<ReadyMainCharacter, ReadyCharacter>,
        IReadyConvertable<ReadyMainCharacter, ReadyActor>,
        IReadyConvertable<ReadyMainCharacter, ReadyObject>
{
    internal WukongClientApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyMainCharacter(WukongClientApi api, Entity entity)
    {
        Api = api;
        Entity = entity;
    }

    public static implicit operator ReadyObject(ReadyMainCharacter mainCharacter)
        => new(mainCharacter.Api, mainCharacter.Entity);

    public static explicit operator ReadyMainCharacter(ReadyObject obj)
    {
        if (!MainCharacterEntity.IsMainCharacter(obj.Entity))
            throw new InvalidCastException($"The provided {nameof(ReadyObject)} is not {nameof(ReadyMainCharacter)}.");
        return new ReadyMainCharacter(obj.Api, obj.Entity);
    }

    public static implicit operator ReadyCharacter(ReadyMainCharacter mainCharacter)
        => new(mainCharacter.Api, mainCharacter.Entity);

    public static explicit operator ReadyMainCharacter(ReadyCharacter character)
    {
        if (!MainCharacterEntity.IsMainCharacter(character.Entity))
            throw new InvalidCastException($"The provided {nameof(ReadyCharacter)} is not {nameof(MainCharacterEntity)}.");
        return new(character.Api, character.Entity);
    }

    ReadyMainCharacter IReadyEntity<ReadyMainCharacter>.Construct(WukongClientApi api, Entity entity)
        => new(api, entity);

    void IReadyEntity<ReadyMainCharacter>.Deconstruct(out WukongClientApi api, out Entity entity)
    {
        api = Api;
        entity = Entity;
    }

    // ---

    public PlayerId PlayerId
    {
        get
        {
            var mainEntity = new MainCharacterEntity(Entity);
            return mainEntity.GetState().PlayerId;
        }
    }

    public bool IsWaitingForSequence
    {
        get
        {
            var mainEntity = new MainCharacterEntity(Entity);
            return mainEntity.GetLocalState().IsWaitingForSequence;
        }
    }

    public int WaitingSequenceId
    {
        get
        {
            var mainEntity = new MainCharacterEntity(Entity);
            return mainEntity.GetState().WaitingSequenceId;
        }
    }

    public bool IsRespawning
    {
        get
        {
            var mainEntity = new MainCharacterEntity(Entity);
            return mainEntity.GetLocalState().IsRespawning;
        }
    }


    public bool IsTransformed
    {
        get
        {
            var mainEntity = new MainCharacterEntity(Entity);
            return mainEntity.GetState().IsTransformed;
        }
    }

    public int RebirthPointId
    {
        get
        {
            var mainEntity = new MainCharacterEntity(Entity);
            return mainEntity.GetState().RebirthPointId;
        }
    }

    // ---

    public void Teleport(Vector3 location, Vector3 rotation)
    {
        Api.MappedEvent.InvokeInGameAndNotifyEcs(new RequestTeleportEvent(
            entity: Entity,
            location: location.ToFVector(),
            rotation: rotation.ToFRotator()
        ), default(EmptyContext));
    }

    public void RebirthInPlace()
    {
        var mainEntity = new MainCharacterEntity(Entity);

        Api.MappedEvent.InvokeInGameAndNotifyEcs(new RebirthPlayerEvent(mainEntity.Entity, false), default(EmptyContext));
    }

    public void Respawn(int maxComp)
    {
        var mainEntity = new MainCharacterEntity(Entity);
        var localMainComp = mainEntity.GetLocalState();

        localMainComp.IsRespawning = true;
        Api.MappedEvent.InvokeInGameAndNotifyEcs(new PartyRespawnEvent(
            entity: mainEntity.Entity,
            birthShrineId: maxComp
        ), default(EmptyContext));
    }
}