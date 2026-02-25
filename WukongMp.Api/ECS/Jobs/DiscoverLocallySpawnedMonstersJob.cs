using b1;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Multiplayer.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Jobs;

/// <summary>
/// This job is used to discover already spawned monsters.
/// </summary>
public class DiscoverLocallySpawnedMonstersJob(IMappedEventManager mappedEvent, ILogger logger)
{
    public void OnUpdate(ref LocalTamerComponent localTamerComp, ref MetadataComponent metaComp, Entity entity)
    {
        var tamerEntity = new TamerEntity(entity);
        var tamer = tamerEntity.Tamer;
        if (tamer?.GetMonster() != null)
        {
            logger.LogDebug("Monster {Guid} is already spawned", BGU_DataUtil.GetActorGuid(tamer));
            TamerUtils.MarkMonsterLocallySpawned(mappedEvent, tamerEntity);
        }
    }
}
