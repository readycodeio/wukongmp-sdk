using System;
using System.Diagnostics.CodeAnalysis;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Mapping;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.Mapping;

public class WukongMappingPolicyDirectory(
    IMappingPolicyDirectory policyDir,
    IMappedEntityManager<AActor> mappedEntity,
    MappedEventManager mappedEvent,
    ClientWukongArchetypeRegistration wukongArchetype
)
{
    public MappedEventManager MappedEvent => mappedEvent;

    public bool IsCharacterMapped([NotNullWhen(true)] AActor? character, [NotNullWhen(true)] out Entity? entity)
    {
        if (character.IsNullOrDestroyed())
        {
            entity = null;
            return false;
        }

        if (IsMainCharacterMapped(character, out var mainEntity))
        {
            entity = mainEntity.Value.Entity;
            return true;
        }

        if (IsTamerMapped(character as BUTamerActor, out var tamerEntity))
        {
            entity = tamerEntity.Value.Entity;
            return true;
        }

        if (IsMonsterTamerMapped(character as BGUCharacterCS, out tamerEntity))
        {
            entity = tamerEntity.Value.Entity;
            return true;
        }

        entity = null;
        return false;
    }

    public bool IsCharacterMapped([NotNullWhen(true)] AActor? character, [NotNullWhen(true)] out Entity? entity, [NotNullWhen(true)] out ArchetypeId? archetype)
    {
        if (character.IsNullOrDestroyed())
        {
            entity = null;
            archetype = null;
            return false;
        }

        if (IsMainCharacterMapped(character, out var mainEntity))
        {
            entity = mainEntity.Value.Entity;
            archetype = wukongArchetype.MainCharacterArchetype;
            return true;
        }

        if (IsTamerMapped(character as BUTamerActor, out var tamerEntity))
        {
            entity = tamerEntity.Value.Entity;
            archetype = wukongArchetype.TamerArchetype;
            return true;
        }

        if (IsMonsterTamerMapped(character as BGUCharacterCS, out tamerEntity))
        {
            entity = tamerEntity.Value.Entity;
            archetype = wukongArchetype.TamerArchetype;
            return true;
        }

        entity = null;
        archetype = null;
        return false;
    }

    public bool IsMainCharacterMapped([NotNullWhen(true)] AActor? character, [NotNullWhen(true)] out MainCharacterEntity? mainEntity)
    {
        if (character.IsNullOrDestroyed())
        {
            mainEntity = null;
            return false;
        }

        if (!mappedEntity.IsMapped(character, out var entity))
        {
            mainEntity = null;
            return false;
        }

        var archetype = entity.Value.GetComponent<MetadataComponent>().Archetype;
        if (archetype != wukongArchetype.MainCharacterArchetype)
        {
            mainEntity = null;
            return false;
        }

        mainEntity = new(entity.Value);

        if (mainEntity.Value.Pawn != character)
        {
            mainEntity = null;
            return false;
        }

        return true;
    }

    public bool IsTamerMapped([NotNullWhen(true)] BUTamerActor? tamer, [NotNullWhen(true)] out TamerEntity? tamerEntity)
    {
        if (tamer.IsNullOrDestroyed())
        {
            tamerEntity = null;
            return false;
        }

        if (!mappedEntity.IsMapped(tamer, out var entity))
        {
            tamerEntity = null;
            return false;
        }

        var archetype = entity.Value.GetComponent<MetadataComponent>().Archetype;
        if (archetype != wukongArchetype.TamerArchetype)
        {
            tamerEntity = null;
            return false;
        }

        tamerEntity = new(entity.Value);

        if (tamer != tamerEntity.Value.Tamer)
        {
            tamerEntity = null;
            return false;
        }

        return true;
    }

    public bool IsMonsterTamerMapped([NotNullWhen(true)] BGUCharacterCS? monsterCharacter, [NotNullWhen(true)] out TamerEntity? tamerEntity)
    {
        if (monsterCharacter.IsNullOrDestroyed())
        {
            tamerEntity = null;
            return false;
        }

        var tamerOwner = monsterCharacter?.GetTamerOwner();

        if (!mappedEntity.IsMapped(tamerOwner, out var entity))
        {
            tamerEntity = null;
            return false;
        }

        var archetype = entity.Value.GetComponent<MetadataComponent>().Archetype;
        if (archetype != wukongArchetype.TamerArchetype)
        {
            tamerEntity = null;
            return false;
        }

        tamerEntity = new(entity.Value);

        if (monsterCharacter != tamerEntity.Value.Pawn)
        {
            tamerEntity = null;
            return false;
        }

        return true;
    }

    public IMappingDataPolicy<Entity> ForData<TComponent>()
        where TComponent : struct, IMappingContext<Entity>
        => policyDir.ForData<TComponent>();

    public IMappingDataPolicy<TContext> ForData<TComponent, TContext>()
        where TComponent : struct, IMappingContext<TContext>
        => policyDir.ForData<TComponent, TContext>();

    public IMappingEventPolicy<Entity> ForEvent<TEvent>()
        where TEvent : struct, IEquatable<TEvent>, IMappingContext<Entity>
        => policyDir.ForEvent<TEvent>();

    public IMappingEventPolicy<TContext> ForEvent<TEvent, TContext>()
        where TEvent : struct, IEquatable<TEvent>, IMappingContext<TContext>
        where TContext : struct
        => policyDir.ForEvent<TEvent, TContext>();
}