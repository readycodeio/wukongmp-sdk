using b1;
using BtlShare;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;

namespace WukongMp.Api.ECS.Systems;

public sealed class SyncMonstersSystem : SystemBase
{
    private static bool IsMasterClient => WukongMP.Instance.Client.IsMasterClient;

    public override void OnUpdate()
    {
        Entities.ForEach((
            EntityId _,
            ref HpComponent hpComp,
            ref TeamComponent teamComp,
            ref TamerComponent tamer,
            ref LocalTamerComponent localTamer) =>
        {
            if (localTamer.IsMonsterSpawned || !tamer.IsSpawned)
            {
                return;
            }

            var monster = localTamer.Tamer?.GetMonster();
            if (monster == null)
            {
                var bgsEvents = BGS_EventCollectionCS.Get(localTamer.Tamer);
                if (bgsEvents == null)
                {
                    Logging.LogError("events are null");
                    return;
                }

                bgsEvents.Evt_TamerBlockingSpawnImmediately.Invoke(tamer.Guid);
            }

            monster = localTamer.Tamer?.GetMonster();
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
                    Logging.LogDebug("Monster {Guid} HP scaling set to {Scaling}x", tamer.Guid, hpComp.HpMult);
                }
            }

            var events = BUS_EventCollectionCS.Get(localTamer.Tamer);
            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            IBUC_ABPMotionMatchingData mmData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_ABPMotionMatchingData>(localTamer.Pawn);
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

            localTamer.IsMonsterSpawned = true;
            Logging.LogDebug("Monster {Guid} synced", tamer.Guid);
        });
    }
}