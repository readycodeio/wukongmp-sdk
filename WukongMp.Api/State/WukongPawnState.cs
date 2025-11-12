using System.Diagnostics.CodeAnalysis;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

public class WukongPawnState(
    Store world,
    ClientWukongArchetypeRegistration wukongArchetype,
    ClientNetworkedEntityState netEntity
)
{
    public Entity CreateNetworkedMonster(LocalTamerComponent localTamer, TamerComponent tamer, TeamComponent team)
    {
        var (entity, netId) = netEntity.CreateNetworkedAreaEntity(wukongArchetype.MonsterArchetype, b =>
        {
            b.Add(localTamer);
            b.Add(tamer);
            b.Add(team);
        });
        Logging.LogDebug("Creating local networked monster with {NetId}", netId);
        return entity;
    }

    public bool IsTamerEntity(Entity entity)
    {
        return entity.HasComponent<TamerComponent>();
    }

    public bool IsMainCharacterEntity(Entity entity)
    {
        return entity.HasComponent<MainCharacterComponent>();
    }

    public bool TryGetTamerEntity(Entity entity, [NotNullWhen(true)] out TamerEntity? tamerEntity)
    {
        tamerEntity = null;
        if (!IsTamerEntity(entity))
            return false;

        tamerEntity = new TamerEntity(entity);
        return true;
    }

    public bool TryGetMainCharacterEntity(Entity entity, [NotNullWhen(true)] out MainCharacterEntity? mainCharacterEntity)
    {
        mainCharacterEntity = null;
        if (!IsMainCharacterEntity(entity))
            return false;

        mainCharacterEntity = new MainCharacterEntity(entity);
        return true;
    }

    public BGUCharacterCS? GetPawnByNetworkId(NetworkId netId)
    {
        if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            return null;

        if (entity.Value.TryGetComponent<LocalTamerComponent>(out var localTamer))
            return localTamer.Pawn;

        if (entity.Value.TryGetComponent<LocalMainCharacterComponent>(out var localMain))
            return localMain.Pawn;

        return null;
    }

    public TamerEntity? GetEntityByTamerMonster(AActor? actor)
    {
        if (actor == null)
            return null;

        TamerEntity? result = null;

        var query = world.Query<LocalTamerComponent>();
        query.ForEachEntity((ref LocalTamerComponent localTamerComp, Entity entity) =>
        {
            if (localTamerComp.Pawn == actor)
            {
                result = new TamerEntity(entity);
            }
        });

        return result;
    }

    public TamerEntity? GetEntityByTamerGuid(string guid)
    {
        TamerEntity? result = null;

        var query = world.Query<TamerComponent>();
        query.ForEachEntity((ref TamerComponent tamerComp, Entity entity) =>
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
        if (owner == null)
            return null;

        TamerEntity? result = null;

        var query = world.Query<LocalTamerComponent>();
        query.ForEachEntity((ref LocalTamerComponent localTamerComp, Entity entity) =>
        {
            if (localTamerComp.Tamer == owner)
            {
                result = new TamerEntity(entity);
            }
        });

        return result;
    }

    public MainCharacterEntity? GetEntityByPlayerPawn(AActor? owner)
    {
        if (owner == null)
            return null;

        MainCharacterEntity? result = null;

        var query = world.Query<LocalMainCharacterComponent>();
        query.ForEachEntity((ref LocalMainCharacterComponent localMainComp, Entity entity) =>
        {
            if (!localMainComp.HasPawn)
                return;

            if (localMainComp.Pawn == owner)
                result = new MainCharacterEntity(entity);
        });

        return result;
    }

    public NetworkId? GetNetworkIdByActor(AActor? owner)
    {
        var playerEntity = GetEntityByPlayerPawn(owner);
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

    public bool TryGetEnityByCharacter(BGUCharacterCS? character, [NotNullWhen(true)] out Entity? entity)
    {
        entity = null;
        if (character == null)
            return false;
        var mainCharacterEntity = GetEntityByPlayerPawn(character);
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