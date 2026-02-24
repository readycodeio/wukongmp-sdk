using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Data;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.Mapping.Data;

public readonly struct MainCharacterMappingDataPolicy(IMappingDataPolicy<Entity> dataPolicy)
{
    public bool ShouldGameCopyToEcs(MainCharacterEntity? mainEntity)
    {
        if (!mainEntity.HasValue)
            return false;

        return dataPolicy.ShouldGameCopyToEcs(mainEntity.Value.Entity);
    }
    
    public bool ShouldEcsCopyToGame(MainCharacterEntity? mainEntity)
    {
        if (!mainEntity.HasValue)
            return false;

        return dataPolicy.ShouldEcsCopyToGame(mainEntity.Value.Entity);
    }

    public bool ShouldGameSetLocally(MainCharacterEntity? mainEntity)
    {
        if (!mainEntity.HasValue)
            return false;

        return dataPolicy.ShouldGameSetLocally(mainEntity.Value.Entity);
    }
}