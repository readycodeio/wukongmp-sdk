using System.Collections.Generic;
using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Old.Api;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public sealed class SyncMonstersSystem(IRelayClient relayClient) : QuerySystem<HpComponent, TeamComponent, TamerComponent, LocalTamerComponent>
{
    private bool IsMasterClient => relayClient.IsMasterClient;

    private HashSet<string> NotYetSpawnedGuids = [];

    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref hpComp, ref teamComp, ref tamerComp, ref localTamerComp, entity) =>
        {
            if (localTamerComp.IsMonsterSynced || !tamerComp.ShouldBeSpawned)
            {
                return;
            }

            var currentPhase = localTamerComp.Tamer?.CurrentRef.Phase;
            var monster = localTamerComp.Tamer?.GetMonster();
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                TamerUtils.SpawnMonsterLocally(entity);
            }

            monster = localTamerComp.Tamer?.GetMonster();
            currentPhase = localTamerComp.Tamer?.CurrentRef.Phase;
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                if (NotYetSpawnedGuids.Add(tamerComp.Guid))
                {
                    Logging.LogError("Monster {Guid} not yet spawned, waiting...", tamerComp.Guid);
                }

                return;
            }

            // set monster hp
            var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(monster);

            if (attrs != null)
            {
                if (IsMasterClient)
                {
                    hpComp.HpMaxBase = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                    hpComp.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
                }
#if TESTING
                hpComp.Hp = 10;
                attrs.SetFloatValue(EBGUAttrFloat.Hp, hpComp.Hp);
                attrs.SetFloatValue(EBGUAttrFloat.Shield, 1);
                attrs.SetFloatValue(EBGUAttrFloat.SkillSuperArmor, 1);
                attrs.SetFloatValue(EBGUAttrFloat.BlockCollapseArmor, 1);
#endif
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

            if (!IsMasterClient)
            {
                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                Logging.LogDebug("Tamer actor disabled, guid: {Guid}.", tamerComp.Guid);
            }

            ClientUtils.RegisterNewPlayerTeam(monster, teamComp.TeamId);

            localTamerComp.IsMonsterSynced = true;
            Logging.LogDebug("Monster {Guid} synced", tamerComp.Guid);
        });
    }
}