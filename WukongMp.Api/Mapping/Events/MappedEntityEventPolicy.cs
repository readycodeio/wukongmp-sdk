using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.Mapping.Events;

public readonly struct MappedEntityEventPolicy(IMappingEventPolicy<Entity> dataPolicy)
{
    public bool ShouldEventPropagateToEcs(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldEventPropagateToEcs(tamerEntity.Value);
    }

    [Obsolete("Is this event needed in the API?")]
    public bool ShouldEventPropagateToGame(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldEventPropagateToGame(tamerEntity.Value);
    }

    public bool ShouldGameEventRunLocally(Entity? tamerEntity, out EventSource source)
    {
        if (!tamerEntity.HasValue)
        {
            source = default;
            return false;
        }

        return dataPolicy.ShouldGameEventRunLocally(tamerEntity.Value, out source);
    }
}