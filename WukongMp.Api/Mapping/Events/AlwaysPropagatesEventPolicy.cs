using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.Mapping.Events;

public class AlwaysPropagatesEventPolicy<TEvent>(DataSideChannel sideChannel) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool ShouldEventPropagateToEcsImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool ShouldEventPropagateToGameImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool ShouldGameEventRunLocallyImpl(in EmptyContext context)
    {
        return true;
    }
}