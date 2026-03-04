using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class MasterClientEventPolicy<TEvent>(
    WukongAreaState areaState,
    DataSideChannel sideChannel
) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in EmptyContext context)
    {
        return areaState.IsMasterClient;
    }

    protected override bool CanEcsInvokeGameEventImpl(in EmptyContext context)
    {
        return !areaState.IsMasterClient;
    }

    protected override bool CanGameEventRunLocallyImpl(in EmptyContext context)
    {
        return areaState.IsMasterClient;
    }
}