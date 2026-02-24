using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Data;
using ReadyM.Relay.Common.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Data;

public class MasterClientDataPolicy(WukongAreaState areaState) : IMappingDataPolicy<EmptyContext>
{
    public Type ContextType
        => typeof(Entity);

    public bool ShouldEcsCopyToGame(in EmptyContext context)
        => !areaState.IsMasterClient;

    public bool ShouldGameCopyToEcs(in EmptyContext context)
        => areaState.IsMasterClient;

    public bool ShouldGameSetLocally(in EmptyContext context)
        => true;
}