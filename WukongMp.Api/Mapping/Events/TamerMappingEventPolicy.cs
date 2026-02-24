using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.Mapping.Events;

public readonly struct TamerMappingEventPolicy(IMappingEventPolicy<Entity> dataPolicy)
{
    public bool ShouldEventPropagateToEcs(TamerEntity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldEventPropagateToEcs(tamerEntity.Value.Entity);
    }
    
    public bool ShouldEventPropagateToGame(TamerEntity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldEventPropagateToGame(tamerEntity.Value.Entity);
    }

    public bool ShouldGameEventRunLocally(TamerEntity? tamerEntity, out EventSource source)
    {
        if (!tamerEntity.HasValue)
        {
            source = default;
            return false;
        }

        return dataPolicy.ShouldGameEventRunLocally(tamerEntity.Value.Entity, out source);
    }
}