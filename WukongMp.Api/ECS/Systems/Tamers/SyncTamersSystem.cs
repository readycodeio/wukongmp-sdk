using System.Linq;
using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

internal sealed class SyncTamersSystem(IMappedEventManager mappedEvent) : QuerySystem<TamerComponent, LocalTamerComponent, TransformComponent, MetadataComponent>
{
    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong tickCounter;

    protected override void OnUpdate()
    {
        if (tickCounter++ % TickInterval != 0)
            return;

        var allTamers =
            UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld())
                .Where(x => x != null)
                .GroupBy(x => x.GetFinalGuid())
                .ToDictionary(g => g.Key, g => g.Last());

        Query.ForEachEntity((ref tamerComp, ref localTamerComp, ref translation, ref metaComp, entity) =>
        {
            if (!localTamerComp.IsTamerSynced)
            {
                if (tamerComp.Guid is null)
                {
                    Logging.LogError("Entity {EntityId} has a TamerComponent with a null Guid. Cannot sync tamer.", entity.Id);
                    return;
                }

                if (allTamers.TryGetValue(tamerComp.Guid, out var actor))
                {
                    var tamerEntity = new TamerEntity(entity);
                    
                    tamerEntity.SetTamer(actor, true);
                    Logging.LogDebug("Found matching tamer with guid: {Guid}", tamerComp.Guid);

                    if (tamerEntity.Tamer?.GetMonster() != null)
                    {
                        Logging.LogDebug("Monster already spawned on the level, guid: {Guid}, netId: {NetId}. Marking as spawned.", tamerComp.Guid, metaComp.NetId);
                        TamerUtils.MarkMonsterLocallySpawned(mappedEvent, tamerEntity);
                    }
                }
                else
                {
                    if (tamerComp is { UnitPath: not null })
                    {
                        SpawningUtils.SpawnUnitLocallyByPath(tamerComp.Guid, tamerComp.UnitPath, translation.Position.ToFVector());
                    }
                }
            }
        });
    }
}