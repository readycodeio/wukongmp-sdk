using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.Mapping.Events;

public class AlwaysPropagatesEventPolicy<TEvent>(DataSideChannel sideChannel) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool CanEcsInvokeGameEventImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool CanGameEventRunLocallyImpl(in EmptyContext context)
    {
        return true;
    }
}