using System;
using b1;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

internal class WukongLevelTransitionConnectionController(
    WukongEventBus eventBus,
    WukongConnectionManager connection
) : IHostedService
{
    public void OnScopeStart()
    {
        eventBus.OnBeginPlayGameplayLevel += OnBeginPlayGameplayLevel;
        eventBus.OnEndPlayGameplayLevel += OnEndPlayGameplayLevel;
    }

    public void Dispose()
    {
        eventBus.OnEndPlayGameplayLevel -= OnEndPlayGameplayLevel;
        eventBus.OnBeginPlayGameplayLevel -= OnBeginPlayGameplayLevel;
    }

    private void OnBeginPlayGameplayLevel()
    {
        var areaId = BGUFuncLibMap.GetCurLevelId(GameUtils.GetWorld());
        if (areaId > ushort.MaxValue)
        {
            throw new InvalidCastException("AreaId is greater than ushort max value");
        }

        connection.JoinArea(new AreaId((ushort)areaId));
    }

    private void OnEndPlayGameplayLevel()
    {
        connection.LeaveArea();
    }
}