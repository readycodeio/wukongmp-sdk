using System;
using System.Diagnostics.CodeAnalysis;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Multiplayer.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Mapping.CreateDestroy;
using WukongMp.Api.Mapping.Data;
using WukongMp.Api.Mapping.Events;

namespace WukongMp.Api.Mapping;

public class WukongMappingPolicyDirectory(
    IMappingPolicyDirectory policyDir,
    IMappedEntityManager<AActor> mappedEntity,
    MappedEventManager mappedEvent,
    ClientWukongArchetypeRegistration wukongArchetype)
{
    public IMappingPolicyDirectory PolicyDir => policyDir;
    public MappedEventManager MappedEvent => mappedEvent;
    
    public bool IsCharacterMapped([NotNullWhen(true)] AActor? character, [NotNullWhen(true)] out Entity? entity)
    {
        if (character.IsNullOrDestroyed())
        {
            entity = null;
            return false;
        }

        if (IsMainCharacterMapped_(character, out var mainEntity))
        {
            entity = mainEntity.Value.Entity;
            return true;
        }

        if (IsTamerMapped_(character as BUTamerActor, out var tamerEntity))
        {
            entity = tamerEntity.Value.Entity;
            return true;
        }

        if (IsMonsterTamerMapped_(character as BGUCharacterCS, out tamerEntity))
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

        if (IsMainCharacterMapped_(character, out var mainEntity))
        {
            entity = mainEntity.Value.Entity;
            archetype = wukongArchetype.MainCharacterArchetype;
            return true;
        }

        if (IsTamerMapped_(character as BUTamerActor, out var tamerEntity))
        {
            entity = tamerEntity.Value.Entity;
            archetype = wukongArchetype.TamerArchetype;
            return true;
        }

        if (IsMonsterTamerMapped_(character as BGUCharacterCS, out tamerEntity))
        {
            entity = tamerEntity.Value.Entity;
            archetype = wukongArchetype.TamerArchetype;
            return true;
        }

        entity = null;
        archetype = null;
        return false;
    }

    public bool IsMainCharacterMapped_([NotNullWhen(true)] AActor? character, [NotNullWhen(true)] out MainCharacterEntity? mainEntity)
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
    
    public bool IsTamerMapped_([NotNullWhen(true)] BUTamerActor? tamer, [NotNullWhen(true)] out TamerEntity? tamerEntity)
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

    public bool IsMonsterTamerMapped_([NotNullWhen(true)] BGUCharacterCS? monsterCharacter, [NotNullWhen(true)] out TamerEntity? tamerEntity)
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
    
    public MainCharacterMappingCreateDeletePolicy MainCharacterCreateDelete()
        => new(policyDir.ForCreateDelete<AActor>(wukongArchetype.MainCharacterArchetype));

    public MappedEntityDataPolicy MainCharacterData<TData>()
        where TData : struct, IMappingContext<Entity>
        => new(policyDir.ForData<TData>(wukongArchetype.MainCharacterArchetype));

    public MappedEntityEventPolicy MainCharacterEvent<TEvent>()
        where TEvent : struct, IMappingContext<Entity>
        => new(policyDir.ForEvent<TEvent>());

    public TamerMappingCreateDeletePolicy TamerCreateDelete()
        => new(policyDir.ForCreateDelete<AActor>(wukongArchetype.TamerArchetype));

    public MappedEntityDataPolicy TamerData<TData>()
        where TData : struct, IMappingContext<Entity>
        => new(policyDir.ForData<TData>(wukongArchetype.TamerArchetype));

    public MappedEntityEventPolicy TamerEvent<TEvent>()
        where TEvent : struct, IMappingContext<Entity>
        => new(policyDir.ForEvent<TEvent>());

    public IMappingDataPolicy<Entity> ForData<TData>(ArchetypeId archetypeId)
        where TData : struct, IMappingContext<Entity>
        => policyDir.ForData<TData>(archetypeId);
    
    public IMappingDataPolicy<TContext> ForData<TData, TContext>(ArchetypeId archetypeId)
        where TData : struct, IMappingContext<TContext>
        where TContext : struct
        => policyDir.ForData<TData, TContext>(archetypeId);

    public IMappingEventPolicy<Entity> ForEvent<TEvent>()
        where TEvent : struct, IEquatable<TEvent>, IMappingContext<Entity>
        => policyDir.ForEvent<TEvent>();

    public IMappingEventPolicy<TContext> ForEvent<TEvent, TContext>()
        where TEvent : struct, IEquatable<TEvent>, IMappingContext<TContext>
        where TContext : struct
        => policyDir.ForEvent<TEvent, TContext>();
}