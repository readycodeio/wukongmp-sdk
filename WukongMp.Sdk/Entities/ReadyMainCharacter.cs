using System;
using System.Numerics;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk.Entities;

public readonly struct ReadyMainCharacter
    : IReadyEntity<ReadyMainCharacter>,
        IReadyConvertable<ReadyMainCharacter, ReadyCharacter>,
        IReadyConvertable<ReadyMainCharacter, ReadyActor>,
        IReadyConvertable<ReadyMainCharacter, ReadyObject>
{
    private IWukongSynchronizationApi Api { get; }
    internal MainCharacterEntity Entity { get; }

    internal ReadyMainCharacter(IWukongSynchronizationApi api, Entity entity)
    {
        Api = api;
        Entity = new MainCharacterEntity(entity);
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
        return new ReadyMainCharacter(character.Api, character.Entity);
    }

    ReadyMainCharacter IReadyEntity<ReadyMainCharacter>.Construct(IWukongSynchronizationApi api, Entity entity)
        => new(api, entity);

    void IReadyEntity<ReadyMainCharacter>.Deconstruct(out IWukongSynchronizationApi api, out Entity entity)
    {
        api = Api;
        entity = Entity;
    }

    // ---

    public PlayerId PlayerId => Entity.GetState().PlayerId;
    public bool IsWaitingForCutscene => Entity.GetLocalState().IsWaitingForSequence;
    public int WaitingCutsceneId => Entity.GetState().WaitingSequenceId;
    public bool IsRespawning => Entity.GetLocalState().IsRespawning;
    public bool IsTransformed => Entity.GetState().IsTransformed;
    public int RebirthPointId => Entity.GetState().RebirthPointId;

    public string Nickname
    {
        get => Entity.GetState().CharacterNickname;
        set
        {
            if (DI.Instance.MappedField.CanSetFromApi<MainCharacterComponent>(Entity, out var set))
                set.SetFromApi(MainCharacterComponent.Fields.CharacterNickname, value);
        }
    }

    public bool BeguilingChantEligible
    {
        get => Entity.GetState().BeguilingChantEligible;
        set
        {
            if (DI.Instance.MappedField.CanSetFromApi<MainCharacterComponent>(Entity, out var set))
                set.SetFromApi(MainCharacterComponent.Fields.BeguilingChantEligible, value);
        }
    }

    public bool IsSpectator => Entity.GetPvP().IsSpectator;

    // ---

    public void Teleport(Vector3 location, Vector3 rotation)
    {
        DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new RequestTeleportEvent(
            entity: Entity,
            location: location.ToFVector(),
            rotation: rotation.ToFRotator()
        ), default(EmptyContext));
    }

    public void RebirthInPlace()
    {
        DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new RebirthPlayerEvent(Entity, false), default(EmptyContext));
    }

    public void RebirthAtShrine(int shrineId)
    {
        ref var localMainComp = ref Entity.GetLocalState();
        localMainComp.IsRespawning = true;

        DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new PartyRespawnEvent(
            entity: Entity,
            birthShrineId: shrineId
        ), default(EmptyContext));
    }

    public void EnableInteraction(bool enabled)
    {
        PlayerUtils.SetPlayerInteractionEnabled(Entity, enabled);
    }
}