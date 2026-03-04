using System;
using System.Diagnostics;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Policies.Data;

public class MasterClientDataPolicyFactory(WukongAreaState areaState) : IMappingDataPolicyFactory
{
    public bool Supports(Type dataType, Type contextType)
        => contextType == typeof(EmptyContext) && typeof(IOwnershipManaged).IsAssignableFrom(dataType);

    public IMappingDataPolicyBase CreatePolicy(ArchetypeId archetypeId, Type dataType, Type contextType)
    {
        var policyType = typeof(MasterClientDataPolicy).MakeGenericType(contextType);
        return (IMappingDataPolicyBase)Activator.CreateInstance(policyType, areaState)!;
    }

    public IMappingDataPolicy<TContext> CreatePolicy<TContext>(ArchetypeId archetypeId, Type dataType)
        where TContext : struct
    {
        Debug.Assert(typeof(TContext) == typeof(EmptyContext));
        return (IMappingDataPolicy<TContext>)CreatePolicy(archetypeId, dataType, typeof(TContext));
    }
}