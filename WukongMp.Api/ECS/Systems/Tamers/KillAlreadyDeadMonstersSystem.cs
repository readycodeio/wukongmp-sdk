using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class KillAlreadyDeadMonstersSystem : QuerySystem<TamerComponent, LocalTamerComponent, MetadataComponent, HpComponent>
{
    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong tickCounter;

    protected override void OnUpdate()
    {
        if (tickCounter++ % TickInterval != 0)
            return;

        Query.ForEachEntity((ref tamerComp, ref localTamerComp, ref metaComp, ref hpComp, _) =>
        {
            if (localTamerComp.IsTamerSynced && hpComp.IsDead)
            {
                var monster = localTamerComp.Tamer?.GetMonster();

                if (monster == null || BGUFunctionLibraryCS.BGUHasUnitState(monster, EBGUUnitState.Dead))
                    return;

                Logging.LogDebug("Monster is dead, sending unitDead locally. Guid: {Guid}, netId: {NetId}.", tamerComp.Guid, metaComp.NetId);
                BUS_EventCollectionCS.Get(monster)?.Evt_UnitDead.Invoke(monster, EDeadReason.SkillDamage);
            }
        });
    }
}