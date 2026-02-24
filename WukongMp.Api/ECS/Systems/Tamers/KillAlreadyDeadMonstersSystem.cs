using System.Diagnostics;
using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class KillAlreadyDeadMonstersSystem(
    WukongMappingPolicyDirectory policyDir,
    // NOTE(api): API refactoring only
    ClientOwnershipManager clientOwnership, 
    WukongPlayerState playerState,
    ILogger logger) : QuerySystem<TamerComponent, LocalTamerComponent, MetadataComponent, HpComponent>
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
            
            if (localTamerComp is { IsCheckedForDead: false, IsTamerSynced: true } && hpComp.IsDead)
            {
                if (policyDir.TamerData<HpComponent>().ShouldEcsCopyToGame(tamerEntity))
                {
                    // NOTE(api): API refactoring only
                    Debug.Assert(!clientOwnership.OwnsEntity(entity));
                    
                    var tamer = tamerEntity.Tamer;
                    var monster = tamer?.GetMonster();

                    if (monster == null || tamer?.CurrentRef?.Phase != ETamerPhase.Spawned || BGUFunctionLibraryCS.BGUHasUnitState(monster, EBGUUnitState.Dead))
                        return;

                    logger.LogDebug("Monster is dead, sending unitDead locally. Guid: {Guid}, netId: {NetId}.", tamerComp.Guid, metaComp.NetId);

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
                else
                {
                    // NOTE(api): API refactoring only
                    Debug.Assert(clientOwnership.OwnsEntity(entity));
                }
            }
        });
    }
}