using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;

namespace WukongMp.Api;

public class EntityManagerWithLogs(NetworkedEntityManager netManager)
{
    public (Entity Entity, NetworkIdComponent NetId) CreateNetworkedGlobalEntity(
        ArchetypeId archetype,
        Action<EntityBuilder>? setComponents = null)
    {
        var (entity, netId) = netManager.CreateNetworkedGlobalEntity(archetype, setComponents);
        Logging.LogDebug("Networked entity created: {Id} (owned, global)", netId);
        return (entity, netId);
    }
    
    public (Entity Entity, NetworkIdComponent NetId) CreateNetworkedEntity(
        ArchetypeId archetype,
        Entity scopeEntity,
        Action<EntityBuilder>? setComponents = null)
    {
        var (entity, netId) = netManager.CreateNetworkedEntity(archetype, scopeEntity, setComponents);
        Logging.LogDebug("Networked entity created: {Id} (owned)", netId);
        return (entity, netId);
    }

    public bool TryGetEntityByNetworkId(NetworkIdComponent netEntity, [NotNullWhen(true)] out Entity? entity)
    {
        return netManager.TryGetEntityByNetworkId(netEntity, out entity);
    }
}
