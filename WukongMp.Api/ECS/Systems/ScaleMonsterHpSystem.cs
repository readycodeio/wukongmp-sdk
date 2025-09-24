using b1;
using BtlShare;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System;
using Microsoft.Extensions.Logging;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems;

public class ScaleMonsterHpSystem : QuerySystem<HpComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        var areaPlayers = DI.Instance.State.AreaPlayers.Count;

#if DEBUG
        const float targetScaling = .5f;
#else
        var targetScaling = 1 + 1.5f * (areaPlayers - 1);
#endif
        Query.ForEachEntity((ref HpComponent hp, ref LocalTamerComponent localTamer, Entity entity) =>
        {
            if (!localTamer.IsMonsterActive)
                return;

            if (!DI.Instance.ClientOwnership.OwnsEntity(entity))
                return;

            if (hp.Hp.Equals(0, Constants.FloatComparisonTolerance) && hp.HpMaxBase.Equals(0, Constants.FloatComparisonTolerance))
                return; // no need to scale if monster is not active

            if (Math.Abs(targetScaling - hp.HpMultiplier) > Constants.FloatComparisonTolerance)
            {
                if (localTamer.Pawn == null)
                    return;

                var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(localTamer.Pawn);

                if (attrs == null)
                {
                    DI.Instance.Logger.LogWarning("Failed to get AttrContainer for entity {Entity}", entity);
                    return;
                }

                var currentHp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
                var maxHp = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);

                hp.HpMaxBase = maxHp / hp.HpMultiplier * targetScaling;
                hp.Hp = currentHp / hp.HpMultiplier * targetScaling;

                attrs.SetFloatValue(EBGUAttrFloat.HpMaxBase, hp.HpMaxBase);
                attrs.SetFloatValue(EBGUAttrFloat.Hp, hp.Hp);

                hp.HpMultiplier = targetScaling;

                DI.Instance.Logger.LogDebug("Scaled monster HP to {Hp}/{HpMaxBase} (x{Multiplier}) for {Players} players", hp.Hp, hp.HpMaxBase, targetScaling, areaPlayers);
            }
        });
    }
}