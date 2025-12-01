using System.Collections.Generic;
using b1;
using BtlShare;
using CSharpModBase;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

/// <summary>
/// Spawns pawns for monsters that do not correspond to any current scene pawn. Tamers have local state that indicates
/// whether they require spawning.
/// </summary>
/// <param name="state"></param>
public sealed class SpawnTamersSystem(ClientState state, GameplayEventRouter router, GameplayConfiguration configuration) : QuerySystem<MetadataComponent, HpComponent, TeamComponent, TamerComponent, LocalTamerComponent>
{
    private readonly HashSet<string?> _notYetSpawnedGuids = [];

    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref metaComp,
            ref hpComp,
            ref teamComp,
            ref tamerComp,
            ref localTamerComp, entity) =>
        {
            // FIXME: Are some of those flags supposed to be removed now that all monsters are in ECS (including the
            // ones spawned in PVP?)
            if (localTamerComp.IsMonsterActive || !tamerComp.ShouldBeSpawned)
            {
                return;
            }

            if (!localTamerComp.IsTamerSynced || localTamerComp.Tamer == null)
            {
                return;
            }

            if (localTamerComp.Tamer.CurrentRef?.Phase == ETamerPhase.Dead)
            {
                return;
            }

            var monster = localTamerComp.Tamer?.GetMonster();
            var currentPhase = localTamerComp.Tamer!.CurrentRef?.Phase;
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                TamerUtils.SpawnMonsterLocally(new TamerEntity(entity));
            }

            monster = localTamerComp.Tamer.GetMonster();
            currentPhase = localTamerComp.Tamer.CurrentRef?.Phase;
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                if (_notYetSpawnedGuids.Add(tamerComp.Guid))
                {
                    Logging.LogInformation("Monster {Guid} not yet spawned, waiting...", tamerComp.Guid);
                }

                return;
            }

            // set monster hp
            var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(monster);

            if (attrs != null)
            {
                if (DI.Instance.ClientOwnership.OwnsEntity(entity))
                {
                    hpComp.HpMaxBase = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                    hpComp.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
                    if (configuration.SyncTamerTeamFromGameToEcs)
                        teamComp.TeamId = monster.GetTeamIDInCS();
#if TESTING
                    hpComp.Hp = 10;
                    attrs.SetFloatValue(EBGUAttrFloat.Hp, hpComp.Hp);
#endif
                }
            }

            var events = BUS_EventCollectionCS.Get(localTamerComp.Tamer);
            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            IBUC_ABPMotionMatchingData mmData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPMotionMatchingData>(localTamerComp.Pawn);
            if (mmData != null)
            {
                events.Evt_ChangeMotionMatchingState.Invoke(mmData.DefaultMMState);
            }

            if (tamerComp.Guid == "UGuid.HYS.JiRuHuo01")
            {
                Utils.TryRunOnGameThread(() =>
                {
                    MarkerUtils.cube1 ??= MarkerUtils.SpawnMarkerActor();
                    MarkerUtils.cube1?.CallFunctionByNameWithArguments($"SetText Actor (R=0.3,G=0.3,B=1)", true);
                    MarkerUtils.cube2 ??= MarkerUtils.SpawnMarkerActor();
                    MarkerUtils.cube2?.CallFunctionByNameWithArguments($"SetText Mesh (R=0.3,G=1,B=0.3)", true);
                    MarkerUtils.cube3 ??= MarkerUtils.SpawnMarkerActor();
                    MarkerUtils.cube3?.CallFunctionByNameWithArguments($"SetText Head (R=1,G=0.3,B=0.3)", true);
                    MarkerUtils.cube4 ??= MarkerUtils.SpawnMarkerActor();
                    MarkerUtils.cube4?.CallFunctionByNameWithArguments($"SetText CameraLock (R=1,G=1,B=1)", true);
                    MarkerUtils.cube5 ??= MarkerUtils.SpawnMarkerActor();
                    MarkerUtils.cube5?.CallFunctionByNameWithArguments($"SetText Root (R=1,G=1,B=1)", true);
                });
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

            if (localTamerComp.Tamer.TamerType == ETamerType.Spawned)
            {
                router.RaiseOnMonsterSpawned(entity);
            }

            Logging.LogDebug("Monster {Guid} synced", tamerComp.Guid);
        });
    }
}