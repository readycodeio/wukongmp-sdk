using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.Mapping.Events;

/// Used with events that are only happening on the game-side and it makes no sense to trigger them manually.
public class AlwaysPropagatesToEcsOnlyEventPolicy<TEvent>(DataSideChannel sideChannel) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool ShouldEventPropagateToEcsImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool ShouldEventPropagateToGameImpl(in EmptyContext context)
    {
        return false;
    }

    protected override bool ShouldGameEventRunLocallyImpl(in EmptyContext context)
    {
        return true;
    }
}