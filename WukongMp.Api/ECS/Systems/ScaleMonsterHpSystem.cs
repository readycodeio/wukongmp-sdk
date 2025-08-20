using b1;
using BtlShare;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems;

public class ScaleMonsterHpSystem : QuerySystem<HpComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        var areaPlayers = DI.Instance.State.AreaPlayers.Count;
        var targetScaling = 1 + 1.5f * (areaPlayers - 1);

        Query.ForEachEntity((ref HpComponent hp, ref LocalTamerComponent localTamer, Entity entity) =>
        {
            if (!localTamer.IsMonsterSynced)
                return;

            if (!DI.Instance.ClientOwnership.OwnsEntity(entity))
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