using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public sealed class SyncMonstersSystem : QuerySystem<HpComponent, TeamComponent, TamerComponent, LocalTamerComponent>
{
    private static bool IsMasterClient => WukongMpMod.Instance.IsMasterClient;

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
            if (monster != null && currentPhase != ETamerPhase.Spawned)
            {
                Logging.LogError("Monster {Guid} is not null but is not yet fully spawned (previously crash)", tamerComp.Guid);
            }

            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                TamerUtils.SpawnMonsterLocally(entity);
            }
            monster = localTamerComp.Tamer?.GetMonster();
            currentPhase = localTamerComp.Tamer?.CurrentRef.Phase;
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                Logging.LogError("Monster not yet spawned");
                return;
            }

            // set monster hp
            var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(monster);

            if (attrs != null)
            {
                hpComp.HpMaxBase = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                hpComp.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
#if TESTING
                hpComp.Hp = 10;
                attrs.SetFloatValue(EBGUAttrFloat.Hp, hpComp.Hp);
                attrs.SetFloatValue(EBGUAttrFloat.Shield, 1);
                attrs.SetFloatValue(EBGUAttrFloat.SkillSuperArmor, 1);
                attrs.SetFloatValue(EBGUAttrFloat.BlockCollapseArmor, 1);
#endif

                if (IsMasterClient && hpComp.HpMult != hpComp.LastMult && hpComp.HpMult != 0)
                {
                    hpComp.HpMaxBase *= hpComp.HpMult;
                    hpComp.Hp *= hpComp.HpMult;

                    attrs.SetFloatValue(EBGUAttrFloat.HpMaxBase, hpComp.HpMaxBase);
                    attrs.SetFloatValue(EBGUAttrFloat.Hp, hpComp.Hp);

                    hpComp.LastMult = hpComp.HpMult;
                    Logging.LogDebug("Monster {Guid} HP scaling set to {Scaling}x", tamerComp.Guid, hpComp.HpMult);
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

            if (!IsMasterClient)
            {
                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                Logging.LogDebug("Tamer actor disabled.");
            }

            ClientUtils.RegisterNewPlayerTeam(monster, teamComp.TeamId);

            localTamerComp.IsMonsterSynced = true;
            Logging.LogDebug("Monster {Guid} synced", tamerComp.Guid);
        });
    }
}