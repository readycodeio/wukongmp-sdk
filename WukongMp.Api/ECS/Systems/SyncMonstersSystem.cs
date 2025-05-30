using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;

namespace WukongMp.Api.ECS.Systems;

public sealed class SyncMonstersSystem : QuerySystem<HpComponent, TeamComponent, TamerComponent, LocalTamerComponent>
{
    private static bool IsMasterClient => WukongMpMod.Instance.IsMasterClient;

    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref hpComp, ref teamComp, ref tamerComp, ref localTamerComp, _) =>
        {
            if (localTamerComp.IsMonsterSpawned || !tamerComp.IsSpawned)
            {
                return;
            }

            var monster = localTamerComp.Tamer?.GetMonster();
            if (monster == null)
            {
                var bgsEvents = BGS_EventCollectionCS.Get(localTamerComp.Tamer);
                if (bgsEvents == null)
                {
                    Logging.LogError("events are null");
                    return;
                }

                bgsEvents.Evt_TamerBlockingSpawnImmediately.Invoke(tamerComp.Guid);
            }

            monster = localTamerComp.Tamer?.GetMonster();
            if (monster == null)
            {
                Logging.LogError("monster is null");
                return;
            }

            // set monster hp
            var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(monster);

            if (attrs != null)
            {
                hpComp.HpMaxBase = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                hpComp.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);

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

            localTamerComp.IsMonsterSpawned = true;
            Logging.LogDebug("Monster {Guid} synced", tamerComp.Guid);
        });
    }
}