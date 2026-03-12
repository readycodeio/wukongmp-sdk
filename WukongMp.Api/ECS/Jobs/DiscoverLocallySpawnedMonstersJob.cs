using b1;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Mapping.Events;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Jobs;

/// <summary>
/// This job is used to discover already spawned monsters.
/// </summary>
internal readonly struct DiscoverLocallySpawnedMonstersJob(IMappedEventManager mappedEvent, ILogger logger)
{
    public void OnUpdate(Entity entity)
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
