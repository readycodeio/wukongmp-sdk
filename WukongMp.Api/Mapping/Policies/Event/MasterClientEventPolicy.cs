using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Policies.Event;

internal class MasterClientEventPolicy<TEvent>(
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