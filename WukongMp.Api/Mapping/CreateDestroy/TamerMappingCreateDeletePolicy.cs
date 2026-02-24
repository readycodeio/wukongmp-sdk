using b1;
using ReadyM.Api.Mapping.CreateDestroy;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.Mapping.CreateDestroy;

public readonly struct TamerMappingCreateDeletePolicy(IMappingCreateDeletePolicy<AActor> policy)
{
    public bool ShouldGameCreatePropagateToEcs(BUTamerActor? tamer)
    {
        if (tamer == null)
            return false;
        
        return policy.ShouldGameCreatePropagateToEcs(tamer);
    }

    public bool ShouldGameCreatePropagateToEcs(BGUCharacterCS? monsterCharacter)
    {
        var tamerOwner = monsterCharacter?.GetTamerOwner();
        if (tamerOwner == null)
            return false;
        
        return policy.ShouldGameCreatePropagateToEcs(tamerOwner);
    }
    
    public bool ShouldGameDeletePropagateToEcs(TamerEntity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;
        
        return policy.ShouldGameDeletePropagateToEcs(tamerEntity.Value.Entity);
    }
}