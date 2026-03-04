using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Policies.Data;

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