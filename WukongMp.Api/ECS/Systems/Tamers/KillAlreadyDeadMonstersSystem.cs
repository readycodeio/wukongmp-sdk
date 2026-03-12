using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.Tamers;

internal sealed class KillAlreadyDeadMonstersSystem(ClientOwnershipManager clientOwnership, WukongPlayerState playerState) : QuerySystem<TamerComponent, LocalTamerComponent, MetadataComponent, HpComponent>
{
    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong tickCounter;

    protected override void OnUpdate()
    {
        if (tickCounter++ % TickInterval != 0)
            return;

        if (playerState.LocalPlayerId == null)
            return;

        Query.ForEachEntity((ref tamerComp, ref localTamerComp, ref metaComp, ref hpComp, entity) =>
        {
            var tamerEntity = new TamerEntity(entity);
            
            if (localTamerComp is { IsCheckedForDead: false, IsTamerSynced: true } && hpComp.IsDead && !clientOwnership.OwnsEntity(entity))
            {
                var monster = tamerEntity.Tamer?.GetMonster();

                if (monster == null || tamerEntity.Tamer?.CurrentRef?.Phase != ETamerPhase.Spawned || BGUFunctionLibraryCS.BGUHasUnitState(monster, EBGUUnitState.Dead))
                    return;

                Logging.LogDebug("Monster is dead, sending unitDead locally. Guid: {Guid}, netId: {NetId}.", tamerComp.Guid, metaComp.NetId);

                if (tamerComp.Guid == "UGuid.LYS.KJL.Women")
                {
                    BUS_EventCollectionCS.Get(monster)?.Evt_UnitDead.Invoke(monster, EDeadReason.SkillDamage, 11213, 5);
                }
                else
                {
                    BUS_EventCollectionCS.Get(monster)?.Evt_UnitDead.Invoke(monster, EDeadReason.SkillDamage);
                }

                localTamerComp.IsCheckedForDead = true; // Check each tamer only once.
            }
        });
    }
}