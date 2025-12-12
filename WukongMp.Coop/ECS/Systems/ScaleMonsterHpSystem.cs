using System;
using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop.ECS.Systems;

public class ScaleMonsterHpSystem : QuerySystem<HpComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        var areaPlayers = DI.Instance.State.AreaPlayers.Count;

        var targetScaling = 1 + 1.5f * (areaPlayers - 1);

#if DEBUG
        if (DebugUtils.ScaleMonsterHpToHalf)
        {
            targetScaling = .5f;
        }
#endif

        Query.ForEachEntity((ref hp, ref localTamer, entity) =>
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

                var info = BGW_GameDB.GetUnitBattleInfoExtendDesc(localTamer.Pawn.GetFinalBattleInfoExtendID());
                if (info == null)
                    return;

                var healthBarType = info.BloodBarType;

                if (healthBarType is not (EBGUBloodBarType.BossBar or EBGUBloodBarType.EliteBar))
                    return;

                var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(localTamer.Pawn);

                if (attrs == null)
                {
                    DI.Instance.Logger.LogWarning("Failed to get AttrContainer for pawn {Pawn}", localTamer.Pawn.GetName());
                    return;
                }

                var currentHp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
                var maxHp = attrs.GetFloatValue(EBGUAttrFloat.HpMaxBase);

                hp.HpMaxBase = maxHp / hp.HpMultiplier * targetScaling;
                hp.Hp = currentHp / hp.HpMultiplier * targetScaling;

                attrs.SetFloatValue(EBGUAttrFloat.HpMaxBase, hp.HpMaxBase);
                attrs.SetFloatValue(EBGUAttrFloat.Hp, hp.Hp);

                hp.HpMultiplier = targetScaling;

                DI.Instance.Logger.LogDebug("Scaled {MonsterType} HP to {Hp}/{HpMaxBase} (x{Multiplier}) for {Players} players", healthBarType, hp.Hp, hp.HpMaxBase, targetScaling, areaPlayers);
            }
        });
    }
}