using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.Mapping.Events;

public readonly struct MappedEntityEventPolicy(IMappingEventPolicy<Entity> dataPolicy)
{
    public bool CanGameEventNotifyEcs(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.CanGameEventNotifyEcs(tamerEntity.Value);
    }

    [Obsolete("Is this event needed in the API?")]
    public bool CanEcsInvokeGameEvent(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.CanEcsInvokeGameEvent(tamerEntity.Value);
    }

    public bool CanGameEventRunLocally(Entity? tamerEntity, out EventSource source)
    {
        if (!tamerEntity.HasValue)
        {
            source = default;
            return false;
        }

        return dataPolicy.CanGameEventRunLocally(tamerEntity.Value, out source);
    }
}