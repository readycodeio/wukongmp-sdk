using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

public sealed class ScaleMonsterHpSystem(WukongLocalApi localApi, WukongClientApi clientApi, ILogger logger)
    : PluginSystemBase(localApi, clientApi, logger)
{
    protected override void OnUpdate(UpdateTick tick)
    {
        var areaPlayers = ClientApi.AreaPlayers.Count;

        var targetScaling = 1 + 1.5f * (areaPlayers - 1);

#if DEBUG
        if (DebugUtils.ScaleMonsterHpToHalf)
        {
            targetScaling = .5f;
        }
#endif

        foreach (var tamer in ClientApi.AllTamers)
        {
            if (!tamer.IsMonsterActive)
                return;

            if (tamer.Owner != ClientApi.LocalPlayerId)
                return;

            if (tamer.Hp.Equals(0f, Constants.FloatComparisonTolerance) && tamer.HpMaxBase.Equals(0, Constants.FloatComparisonTolerance))
                return; // no need to scale if monster is not active

            if (Math.Abs(targetScaling - tamer.HpMultiplier) > Constants.FloatComparisonTolerance)
            {
                if (tamer is { IsBoss: false, IsElite: false })
                    continue;

                var currentHp = tamer.Hp;
                var maxHp = tamer.HpMaxBase;

                ReadyCharacterExtensions.set_HpMaxBase(tamer, maxHp / tamer.HpMultiplier * targetScaling);
                ReadyCharacterExtensions.set_Hp(tamer, currentHp / tamer.HpMultiplier * targetScaling);

                tamer.HpMultiplier = targetScaling;

                var tamerKind = tamer.IsBoss ? "Boss" : "Elite";

                Logger.LogDebug("Scaled {MonsterType} HP to {Hp}/{HpMaxBase} (x{Multiplier}) for {Players} players", tamerKind, tamer.Hp, tamer.HpMaxBase, targetScaling, areaPlayers);
            }
        }
    }
}