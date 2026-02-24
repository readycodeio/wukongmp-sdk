using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Data;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.Mapping.Data;

public readonly struct TamerMappingDataPolicy(IMappingDataPolicy<Entity> dataPolicy)
{
    public bool ShouldGameCopyToEcs(TamerEntity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;
        
        return dataPolicy.ShouldGameCopyToEcs(tamerEntity.Value.Entity);
    }
    
    public bool ShouldEcsCopyToGame(TamerEntity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldEcsCopyToGame(tamerEntity.Value.Entity);
    }

    public bool ShouldGameSetLocally(TamerEntity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldGameSetLocally(tamerEntity.Value.Entity);
    }
}