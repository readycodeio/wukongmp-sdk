using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.Components;

namespace WukongMp.Api.ECS.Systems;

public class ScaleMonsterHpSystem : QuerySystem<HpComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        if (!DI.Instance.RelayClient.IsMasterClient)
            return; // TODO: Ownership check

        var targetMult = 1 + DI.Instance.Players.ConnectedPlayers.Count;

        Query.ForEachEntity((ref hp, ref localTamer, entity) =>
        {
            if (!localTamer.IsMonsterSynced)
                return;

            if (hp.Hp.Equals(0, 0.01f) && hp.HpMaxBase.Equals(0, 0.01f))
                return; // no need to scale if monster is not active

            hp.HpMult = targetMult;

            if (hp.LastMult == 0) // never scaled before
                hp.LastMult = 1;

            if (hp.HpMult != hp.LastMult)
            {
                if (localTamer.Pawn == null)
                    return;

                var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(localTamer.Pawn);
                var currentHp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
                var maxHp = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);

                hp.HpMaxBase = maxHp / hp.LastMult * hp.HpMult;
                hp.Hp = currentHp / hp.LastMult * hp.HpMult;

                attrs.SetFloatValue(EBGUAttrFloat.HpMaxBase, hp.HpMaxBase);
                attrs.SetFloatValue(EBGUAttrFloat.Hp, hp.Hp);

                hp.LastMult = hp.HpMult;
            }
        });
    }
}