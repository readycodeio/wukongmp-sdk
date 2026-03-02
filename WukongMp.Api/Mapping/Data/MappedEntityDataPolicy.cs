using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Data;

namespace WukongMp.Api.Mapping.Data;

public readonly struct MappedEntityDataPolicy(IMappingDataPolicy<Entity> dataPolicy)
{
    public bool ShouldGameCopyToEcs(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;
        
        return dataPolicy.ShouldGameCopyToEcs(tamerEntity.Value);
    }
    
    public bool ShouldEcsCopyToGame(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldEcsCopyToGame(tamerEntity.Value);
    }

    public bool ShouldGameSetLocally(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldGameSetLocally(tamerEntity.Value);
    }
}