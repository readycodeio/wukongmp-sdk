using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public sealed class SyncMonstersSystem(ClientState state) : QuerySystem<MetadataComponent, HpComponent, TeamComponent, TamerComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref metaComp, 
            ref hpComp, 
            ref teamComp, 
            ref tamerComp, 
            ref localTamerComp,
            entity) =>
        {
            if (localTamerComp.IsMonsterSynced || !tamerComp.ShouldBeSpawned)
            {
                return;
            }

            var currentPhase = localTamerComp.Tamer?.CurrentRef.Phase;
            var monster = localTamerComp.Tamer?.GetMonster();
            if (currentPhase != ETamerPhase.Spawned || monster == null)
            {
                TamerUtils.SpawnMonsterLocally(new TamerEntity(entity));
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

                if (metaComp.Owner == state.LocalPlayerId && hpComp.HpMult != hpComp.LastMult && hpComp.HpMult != 0)
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

            if (metaComp.Owner != state.LocalPlayerId)
            {
                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                Logging.LogDebug("Tamer actor disabled, guid: {Guid}.", tamerComp.Guid);
            }

            ClientUtils.RegisterAndSetPlayerTeam(monster, teamComp.TeamId);

            localTamerComp.IsMonsterSynced = true;
            Logging.LogDebug("Monster {Guid} synced", tamerComp.Guid);
        });
    }
}