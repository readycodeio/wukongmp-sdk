using System.Collections.Generic;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

/// <summary>
/// Spawns pawns for monsters that do not correspond to any current scene pawn. Tamers have local state that indicates
/// whether they require spawning.
/// </summary>
/// <param name="state"></param>
public sealed class SpawnTamersSystem(ClientState state, WukongAreaState areaState) : QuerySystem<MetadataComponent, HpComponent, TeamComponent, TamerComponent, LocalTamerComponent>
{
    private readonly HashSet<string?> _notYetSpawnedGuids = [];

    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref MetadataComponent metaComp, 
            ref HpComponent hpComp, 
            ref TeamComponent teamComp, 
            ref TamerComponent tamerComp, 
            ref LocalTamerComponent localTamerComp,
            Entity entity) =>
        {
            if (!localTamerComp.IsTamerSynced || localTamerComp.Tamer == null)
                return;

            // FIXME: Are some of those flags supposed to be removed now that all monsters are in ECS (including the
            // ones spawned in PVP?)
            if (localTamerComp.IsMonsterActive || !tamerComp.ShouldBeSpawned)
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
                    if (Constants.IsCoop)
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

            if (metaComp.Owner != state.LocalPlayerId || (Constants.IsPvP && areaState.PvpState.HasValue && !areaState.PvpState.Value.InPvP))
            {
                TamerUtils.DisableTamer(monster);
            }

            if (Constants.IsPvP && localTamerComp.Tamer.TamerType == ETamerType.Spawned)
            {
                MarkerUtils.CreateMarkerForCharacter(new TamerEntity(entity));
                if (tamerComp.UnitPath == UnitPathsConfig.GetUnitPath(CharacterKind.Monkey))
                {
                    SpawningUtils.SetMonkeyBotConfig(monster);
                }
            }

            localTamerComp.IsMonsterActive = true;
            Logging.LogDebug("Monster {Guid} synced", tamerComp.Guid);
        });
    }
}