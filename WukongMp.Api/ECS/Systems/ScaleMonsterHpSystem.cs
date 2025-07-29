using System;
using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.ECS.Systems;

public class ScaleMonsterHpSystem : QuerySystem<HpComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        if (!DI.Instance.RelayClient.IsMasterClient)
            return; // TODO: Ownership check

        var otherPlayers = DI.Instance.Players.ConnectedPlayers.Count;
        var targetScaling = 1 + 1.5f * otherPlayers;

        Query.ForEachEntity((ref hp, ref localTamer, _) =>
        {
            if (!localTamer.IsMonsterSynced)
                return;

            if (hp.Hp.Equals(0, Constants.FloatComparisonTolerance) && hp.HpMaxBase.Equals(0, Constants.FloatComparisonTolerance))
                return; // no need to scale if monster is not active

            hp.CurrentMultiplier = targetScaling;

            if (Math.Abs(hp.CurrentMultiplier - hp.LastMultiplier) > Constants.FloatComparisonTolerance)
            {
                if (localTamer.Pawn == null)
                    return;

                var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(localTamer.Pawn);
                var currentHp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
                var maxHp = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);

                hp.HpMaxBase = maxHp / hp.LastMultiplier * hp.CurrentMultiplier;
                hp.Hp = currentHp / hp.LastMultiplier * hp.CurrentMultiplier;

                attrs.SetFloatValue(EBGUAttrFloat.HpMaxBase, hp.HpMaxBase);
                attrs.SetFloatValue(EBGUAttrFloat.Hp, hp.Hp);

                hp.LastMultiplier = hp.CurrentMultiplier;
            }
        });
    }
}