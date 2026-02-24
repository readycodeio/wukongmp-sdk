using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Data;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.Mapping.Data;

public class OwnershipDataPolicyFactory(ClientOwnershipManager ownership) : IMappingDataPolicyFactory
{
    public bool Supports(Type dataType, Type contextType)
        => contextType == typeof(Entity) && typeof(IOwnershipManaged).IsAssignableFrom(dataType);

    public IMappingDataPolicyBase CreatePolicy(ArchetypeId archetypeId, Type dataType, Type contextType)
    {
        Debug.Assert(contextType == typeof(Entity));
        return new OwnershipDataPolicy(ownership);
    }

    public IMappingDataPolicy<TContext> CreatePolicy<TContext>(ArchetypeId archetypeId, Type dataType) where TContext : struct
        => (IMappingDataPolicy<TContext>)CreatePolicy(archetypeId, dataType, typeof(TContext));
}