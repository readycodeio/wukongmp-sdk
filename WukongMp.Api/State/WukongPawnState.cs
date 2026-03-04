using System;
using System.Diagnostics.CodeAnalysis;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Mapping;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

public class WukongPawnState(
    Store world,
    MappedEntityManager<AActor> mappedEntity,
    ClientWukongArchetypeRegistration wukongArchetype,
    ClientNetworkedEntityManager netEntity)
{
    public Entity CreateNetworkedTamer(
        LocalTamerComponent localTamerComp, 
        TamerComponent tamerComp, 
        TeamComponent teamComp, 
        BUTamerActor tamer)
    {
        var entity = netEntity.CreateAreaEntity(wukongArchetype.TamerArchetype, b =>
        {
            b.Add(localTamerComp);
            b.Add(tamerComp);
            b.Add(teamComp);
            b.Add(new MappingComponent<AActor>(tamer));
        });
        Logging.LogDebug("Creating local networked monster with {NetId}", entity.GetNetId());
        return entity;
    }

    public BGUCharacterCS? GetPawnByEntity(Entity entity)
    {
        if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
            return tamerEntity.Value.Pawn;

        if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
            return mainEntity.Value.Pawn;

        return null;
    }

    public BGUCharacterCS? GetPawnByNetworkId(NetworkId netId)
    {
        if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            return null;

        return GetPawnByEntity(entity.Value);
    }

    public TamerEntity? GetEntityByTamerMonster(AActor? monster)
    {
        if (monster == null)
            return null;

        TamerEntity? result = null;

        var query = world.Query<LocalTamerComponent>();
        query.ForEachEntity((ref _, entity) =>
        {
            var tamerEntity = new TamerEntity(entity);
            if (tamerEntity.Pawn == monster)
            {
                result = tamerEntity;
            }
        });
        
        return result;
    }

    // FIXME: This should be indexed - move `_guid` to a separate component?
    public TamerEntity? GetEntityByTamerGuid(string guid)
    {
        TamerEntity? result = null;

        var query = world.Query<TamerComponent>();
        query.ForEachEntity((ref tamerComp, entity) =>
        {
            if (tamerComp.Guid == guid)
            {
                result = new TamerEntity(entity);
            }
        });

        return result;
    }

    public TamerEntity? GetEntityByTamer(ABGUTamerBase? owner)
    {
        if (owner.IsNullOrDestroyed())
            return null;

        if (!mappedEntity.IsMapped(owner, out var entity))
            return null;

        if (!TamerEntity.TryGetTamer(entity.Value, out var tamerEntity))
            return null;
        
        return tamerEntity;
    }

    public MainCharacterEntity? GetEntityByPlayerActor(AActor? owner)
    {
        if (owner.IsNullOrDestroyed())
            return null;

        if (!mappedEntity.IsMapped(owner, out var entity))
            return null;

        if (!MainCharacterEntity.TryGetMainCharacter(entity.Value, out var mainEntity))
            return null;
        
        return mainEntity;
    }
    
    [Obsolete]
    public MainCharacterEntity? GetEntityByLastPlayerPawn(AActor? owner)
    {
        if (owner == null)
            return null;

        MainCharacterEntity? result = null;

        var query = world.Query<LocalMainCharacterComponent>();
        query.ForEachEntity((ref localMainComp, entity) =>
        {
            if (localMainComp.LastPawn == owner)
                result = new MainCharacterEntity(entity);
        });

        return result;
    }

    public NetworkId? GetNetworkIdByActor(AActor? owner)
    {
        if (owner.IsNullOrDestroyed())
            return null;
        
        var playerEntity = GetEntityByPlayerActor(owner);
        if (playerEntity.HasValue)
        {
            return playerEntity.Value.GetMeta().NetId;
        }

        var tamerEntity = GetEntityByTamerMonster(owner);
        if (tamerEntity.HasValue)
        {
            return tamerEntity.Value.GetMeta().NetId;
        }

        return null;
    }

    public Entity? GetEntityByActor(AActor? owner)
    {
        if (owner.IsNullOrDestroyed())
            return null;
        
        if (!mappedEntity.IsMapped(owner, out var entity))
            return null;
            
        return entity;
    }
    
    public bool TryGetEntityByCharacter(BGUCharacterCS? character, [NotNullWhen(true)] out Entity? entity)
    {
        entity = null;
        if (character == null)
            return false;
        var mainCharacterEntity = GetEntityByPlayerActor(character);
        if (mainCharacterEntity != null)
        {
            entity = mainCharacterEntity.Value.Entity;
            return true;
        }

        var tamerEntity = GetEntityByTamerMonster(character);
        if (tamerEntity != null)
        {
            entity = tamerEntity.Value.Entity;
            return true;
        }

        return false;
    }
}