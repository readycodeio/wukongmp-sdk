using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Common.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class MasterClientEventPolicy<TEvent>(
    WukongAreaState areaState,
    DataSideChannel sideChannel
) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool ShouldEventPropagateToEcsImpl(in EmptyContext context)
    {
        return areaState.IsMasterClient;
    }

    protected override bool ShouldEventPropagateToGameImpl(in EmptyContext context)
    {
        return !areaState.IsMasterClient;
    }

    protected override bool ShouldGameEventRunLocallyImpl(in EmptyContext context)
    {
        return areaState.IsMasterClient;
    }
}