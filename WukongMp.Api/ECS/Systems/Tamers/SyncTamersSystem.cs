using System.Linq;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class SyncTamersSystem : QuerySystem<TamerComponent, LocalTamerComponent, MetadataComponent, HpComponent>
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

        Query.ForEachEntity((ref TamerComponent tamerComp, ref LocalTamerComponent localTamerComp, ref MetadataComponent metaComp, ref HpComponent hpComp, Entity entity) =>
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
                    localTamerComp.Tamer = actor;
                    localTamerComp.IsTamerSynced = true;
                    Logging.LogDebug("Found matching tamer with guid: {Guid}", tamerComp.Guid);

                    if (hpComp.IsDead)
                    {
                        Logging.LogDebug("Tamer's monster is dead, sending unitDead locally. Guid: {Guid}, netId: {NetId}.", tamerComp.Guid, metaComp.NetId);
                        BUS_EventCollectionCS.Get(actor)?.Evt_UnitDead.Invoke(actor, EDeadReason.SkillDamage);
                    }
                    else if (localTamerComp.Tamer.GetMonster() != null)
                    {
                        Logging.LogDebug("Monster already spawned on the level, guid: {Guid}, netId: {NetId}. Marking as spawned.", tamerComp.Guid, metaComp.NetId);
                        TamerUtils.MarkMonsterLocallySpawned(ref localTamerComp, metaComp);
                    }
                }
            }
        });
    }
}