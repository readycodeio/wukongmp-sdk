using System;
using System.Threading.Tasks;
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

internal sealed class KillAlreadyDeadMonstersSystem : QuerySystem<TamerComponent, LocalTamerComponent, MetadataComponent, HpComponent>, IDisposable
{
    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong tickCounter;

    private bool _enabled;
    private readonly ClientOwnershipManager _clientOwnership;
    private readonly WukongPlayerState _playerState;
    private readonly WukongEventBus _eventBus;

    public KillAlreadyDeadMonstersSystem(ClientOwnershipManager clientOwnership,
        WukongPlayerState playerState,
        WukongEventBus eventBus)
    {
        _clientOwnership = clientOwnership;
        _playerState = playerState;
        _eventBus = eventBus;

        eventBus.OnLoadingScreenClose += EnableWithDelay;
        eventBus.OnLoadingScreenOpen += Disable;
    }

    public void Dispose()
    {
        _eventBus.OnLoadingScreenOpen -= Disable;
        _eventBus.OnLoadingScreenClose -= EnableWithDelay;
    }

    private async void EnableWithDelay()
    {
        try
        {
            await Task.Delay(3000);
            _enabled = true;
            Logging.LogDebug("KillAlreadyDeadMonstersSystem enabled after loading screen.");
        }
        catch
        {
            // unreachable
        }
    }

    private void Disable()
    {
        _enabled = false;
        Logging.LogDebug("KillAlreadyDeadMonstersSystem disabled after gameplay level ended.");
    }

    protected override void OnUpdate()
    {
        if (!_enabled)
            return;

        if (tickCounter++ % TickInterval != 0)
            return;

        if (_playerState.LocalPlayerId == null)
            return;

        Query.ForEachEntity((ref tamerComp, ref localTamerComp, ref metaComp, ref hpComp, entity) =>
        {
            var tamerEntity = new TamerEntity(entity);

            if (localTamerComp is { IsCheckedForDead: false, IsTamerSynced: true } && hpComp.IsDead && !_clientOwnership.OwnsEntity(entity))
            {
                var monster = tamerEntity.Pawn;

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