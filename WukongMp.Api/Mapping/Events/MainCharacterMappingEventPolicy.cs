using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.Mapping.Events;

public readonly struct MainCharacterMappingEventPolicy(IMappingEventPolicy<Entity> dataPolicy)
{
    public bool ShouldEventPropagateToEcs(MainCharacterEntity? mainEntity)
    {
        if (!mainEntity.HasValue)
            return false;

        return dataPolicy.ShouldEventPropagateToEcs(mainEntity.Value.Entity);
    }
    
    public bool ShouldEventPropagateToGame(MainCharacterEntity? mainEntity)
    {
        if (!mainEntity.HasValue)
            return false;

        return dataPolicy.ShouldEventPropagateToGame(mainEntity.Value.Entity);
    }

    public bool ShouldGameEventRunLocally(MainCharacterEntity? mainEntity, out EventSource source)
    {
        if (!mainEntity.HasValue)
        {
            source = default;
            return false;
        }

        return dataPolicy.ShouldGameEventRunLocally(mainEntity.Value.Entity, out source);
    }
}