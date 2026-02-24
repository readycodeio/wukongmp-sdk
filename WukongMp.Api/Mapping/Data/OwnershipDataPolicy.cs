using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Data;
using ReadyM.Relay.Client.State;

namespace WukongMp.Api.Mapping.Data;

public class OwnershipDataPolicy(ClientOwnershipManager ownership) : IMappingDataPolicy<Entity>
{
    public Type ContextType
        => typeof(Entity);
    
    public bool ShouldEcsCopyToGame(in Entity context)
        => !ownership.OwnsEntity(context);

    public bool ShouldGameCopyToEcs(in Entity context)
        => ownership.OwnsEntity(context);

    public bool ShouldGameSetLocally(in Entity context)
        => ownership.OwnsEntity(context);
}