using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;
using Yooni.Native.Container;

namespace WukongMp.Api.ECS.Systems.Tamers;

/// <summary>
/// Spawns pawns for monsters that do not correspond to any current scene pawn. Tamers have local state that indicates
/// whether they require spawning.
/// </summary>
/// <param name="state"></param>
internal sealed class SpawnTamersSystem(ClientState state, GameplayEventRouter router, GameplayConfiguration configuration) : QuerySystem<MetadataComponent, HpComponent, TeamComponent, TamerComponent, LocalTamerComponent>
{
    private readonly HashSet<NativeString256> _notYetSpawnedGuids = [];

    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref metaComp,
            ref hpComp,
            ref teamComp,
            ref tamerComp,
            ref localTamerComp, entity) =>
        {
            // The spawn path below is the only place that loads HpMaxBase, and PatchHp only refreshes HP on an
            // actual HP change, so a monster that is already active and has never been hit keeps Hp = 0 and
            // HpMaxBase = 0 in ECS. Either being missing is enough to need the repair: HP scaling reads both, and
            // a populated HpMaxBase next to Hp = 0 is what makes a scaled boss show up at half health. Everyone who does not own it maps HpComponent the other way, so those zeroes
            // reach their local pawn's attributes and it dies to the first hit. Only the owner can repair this.
            if (localTamerComp is { IsTamerSynced: true, IsMonsterActive: true }
                && (hpComp.HpMaxBase <= 0 || hpComp.Hp <= 0)
                && DI.Instance.ClientOwnership.OwnsEntity(entity))
            {
                var activePawn = new TamerEntity(entity).Tamer?.GetMonster();

                if (activePawn != null && !BGUFunctionLibraryCS.BGUHasUnitState(activePawn, EBGUUnitState.Dead))
                {
                    var activeAttrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(activePawn);

                    if (activeAttrs != null)
                    {
                        LoadHpFromGame(entity, activeAttrs);

                        if (hpComp.HpMaxBase > 0)
                        {
                            Logging.LogDebug("Reloaded HP of active monster {Guid}: {Hp}/{HpMaxBase}.", tamerComp.Guid, hpComp.Hp, hpComp.HpMaxBase);
                        }
                    }
                }
            }

            // FIXME: Are some of those flags supposed to be removed now that all monsters are in ECS (including the
            // ones spawned in PVP?)
            if ((localTamerComp.IsMonsterActive && !hpComp.IsDead) || !tamerComp.ForceKeepSpawned)
            {
                return;
            }
            
            var tamerEntity = new TamerEntity(entity);

            if (!localTamerComp.IsTamerSynced || tamerEntity.Tamer == null)
            {
                return;
            }

            if (tamerEntity.Tamer.CurrentRef?.Phase == ETamerPhase.Dead)
            {
                return;
            }

            var monster = tamerEntity.Tamer?.GetMonster();
            var currentPhase = tamerEntity.Tamer!.CurrentRef?.Phase;
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                TamerUtils.SpawnMonsterLocally(new TamerEntity(entity));
            }

            monster = tamerEntity.Tamer.GetMonster();
            currentPhase = tamerEntity.Tamer.CurrentRef?.Phase;
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                if (_notYetSpawnedGuids.Add(tamerComp.Guid))
                {
                    Logging.LogInformation("Monster {Guid} not yet spawned, waiting...", tamerComp.Guid);
                }

                return;
            }

            if (BGUFunctionLibraryCS.BGUHasUnitState(monster, EBGUUnitState.Dead))
                return;

            // set monster hp
            var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(monster);

            if (attrs != null)
            {
                LoadHpFromGame(entity, attrs);

                if (DI.Instance.ClientOwnership.OwnsEntity(entity))
                {
                    if (configuration.SyncTamerTeamFromGameToEcs)
                        teamComp.TeamId = monster.GetTeamIDInCS();
                }
            }

            var events = BUS_EventCollectionCS.Get(tamerEntity.Tamer);
            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            IBUC_ABPMotionMatchingData mmData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPMotionMatchingData>(tamerEntity.Pawn);
            if (mmData != null)
            {
                events.Evt_ChangeMotionMatchingState.Invoke(mmData.DefaultMMState);
            }

            if (metaComp.Owner != state.LocalPlayerId)
            {
                events.Evt_AIPauseBT.Invoke(true);
                events.Evt_AIPauseFsm.Invoke(true);
                events.Evt_AIPerceptionSetting.Invoke(false);
                Logging.LogDebug("Tamer actor disabled, guid: {Guid}.", tamerComp.Guid);
                if (tamerComp.Guid == "UGuid.HYS.JiRuHuo01")
                {
                    monster.Mesh.SetSimulatePhysics(false);
                    events.Evt_DisablePhysicalMove.Invoke(true);
                }
            }
            else
            {
                var fsmData = BGU_DataUtil.GetReadOnlyData<IBUC_FsmData, BUC_FsmData>(monster);
                if (fsmData != null)
                {
                    Logging.LogDebug("Initial tamer bFsmPaused state {State}, guid: {Guid}.", fsmData.bFsmPaused, tamerComp.Guid);
                    tamerComp.HasFsmPaused = fsmData.bFsmPaused;
                }
            }

            localTamerComp.IsMonsterActive = true;

            if (tamerEntity.Tamer.TamerType == ETamerType.Spawned)
            {
                router.RaiseOnMonsterSpawned(entity);
            }

            Logging.LogDebug("Monster {Guid} synced", tamerComp.Guid);
        });
    }

    /// <summary>
    /// Copies the pawn's real HP into ECS. This is a no-op for anyone who does not own the entity, since the
    /// ownership policy maps <see cref="HpComponent"/> from ECS to the game for them instead.
    /// </summary>
    private static void LoadHpFromGame(Entity entity, BUC_AttrContainer attrs)
    {
        if (!DI.Instance.MappedField.CanLoadFromGame<HpComponent>(entity, out var loader))
            return;

        loader.LoadFromGame(HpComponent.Fields.HpMaxMulPercent.In<BUC_AttrContainer>(), attrs);
        loader.LoadFromGame(HpComponent.Fields.HpMaxBase.In<BUC_AttrContainer>(), attrs);
        loader.LoadFromGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), attrs);
    }
}