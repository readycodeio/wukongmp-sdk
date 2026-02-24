using b1;
using ReadyM.Api.Mapping.CreateDestroy;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.Mapping.CreateDestroy;

public readonly struct MainCharacterMappingCreateDeletePolicy(IMappingCreateDeletePolicy<AActor> policy)
{
    public bool ShouldGameCreatePropagateToEcs(BGUCharacterCS? mainCharacter)
    {
        if (mainCharacter == null)
            return false;
        
        return policy.ShouldGameCreatePropagateToEcs(mainCharacter);
    }
    
    public bool ShouldGameDeletePropagateToEcs(MainCharacterEntity? mainEntity)
    {
        if (!mainEntity.HasValue)
            return false;
        
        return policy.ShouldGameDeletePropagateToEcs(mainEntity.Value.Entity);
    }
}