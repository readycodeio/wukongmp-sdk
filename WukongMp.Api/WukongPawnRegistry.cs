using System.Linq;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;

namespace WukongMp.Api;

public class WukongPawnRegistry
{
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly Store _world;
    private readonly EntityManagerWithLogs _entityManager;
    private readonly ArchetypeId _monsterArchetype;

    public WukongPawnRegistry(WukongPlayerRegistry playerRegistry, Store world, EntityManagerWithLogs entityManager, ISystemRegistry config)
    {
        _playerRegistry = playerRegistry;
        _world = world;
        _entityManager = entityManager;
        
        _monsterArchetype = config.RegisterArchetype(b =>
        {
            WukongCoreApi.RegisterMonsterArchetype(b);
            b.Add<LocalTamerComponent>();
            b.Add<MarkerComponent>();
        });
    }
    
    public Entity CreateNetworkedMonster(LocalTamerComponent localTamer, TamerComponent tamer, TeamComponent team)
    {
        var (entity, netId) = _entityManager.CreateNetworkedEntity(_monsterArchetype, b =>
        {
            b.Add(localTamer);
            b.Add(tamer);
            b.Add(team);
            b.Add(new HpComponent
            {
                CurrentMultiplier = _playerRegistry.AllConnectedPlayers.Count(),
                LastMultiplier = 1f
            });
        });
        Logging.LogDebug("Creating local networked monster with {NetId}", netId);
        return entity;
    }

    public BGUCharacterCS? GetPawnByNetworkId(NetworkIdComponent netId)
    {
        if (netId.Id == uint.MaxValue)
        {
            var player = _playerRegistry.GetPlayerById(netId.Creator);
            if (player != null)
                return player.Pawn;
        }

        if (_entityManager.TryGetEntityByNetworkId(netId, out var entity))
        {
            if (entity.Value.TryGetComponent<LocalTamerComponent>(out var tamer))
            {
                return tamer.Pawn;
            }
        }

        return null;
    }

    public Entity? GetMonsterByActor(AActor? actor)
    {
        if (actor == null)
            return null;

        Entity? entityId = null;

        var query = _world.Query<LocalTamerComponent>();
        query.ThrowOnStructuralChange = false; // okay because the query is readonly
        query.ForEachEntity((ref tamer, entity) =>
        {
            if (tamer.Pawn == actor)
            {
                entityId = entity;
            }
        });

        return entityId;
    }

    public Entity? GetMonsterByGuid(string guid)
    {
        Entity? entityId = null;

        var query = _world.Query<TamerComponent>();
        query.ThrowOnStructuralChange = false; // okay because the query is readonly
        query.ForEachEntity((ref tamer, entity) =>
        {
            if (tamer.Guid == guid)
            {
                entityId = entity;
            }
        });

        return entityId;
    }

    public Entity? GetByTamerActor(BUTamerActor? owner)
    {
        if (owner == null)
            return null;

        Entity? entityId = null;

        var query = _world.Query<LocalTamerComponent>();
        query.ThrowOnStructuralChange = false; // okay because the query is readonly
        query.ForEachEntity((ref tamer, entity) =>
        {
            if (tamer.Tamer == owner)
            {
                entityId = entity;
            }
        });

        return entityId;
    }
}