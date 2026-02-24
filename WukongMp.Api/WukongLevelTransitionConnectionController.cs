using b1;
using System;
using System.Diagnostics;
using ReadyM.Api.Idents;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongLevelTransitionConnectionController : IDisposable
{
    private readonly WukongEventBus _eventBus;
    private readonly WukongConnectionManager _connection;

    public WukongLevelTransitionConnectionController(
        WukongEventBus eventBus,
        WukongConnectionManager connection
    )
    {
        _eventBus = eventBus;
        _connection = connection;
        
        _eventBus.OnBeginPlayGameplayLevel += OnBeginPlayGameplayLevel;
        _eventBus.OnEndPlayGameplayLevel += OnEndPlayGameplayLevel;
    }
    
    public void Dispose()
    {
        _eventBus.OnEndPlayGameplayLevel -= OnEndPlayGameplayLevel;
        _eventBus.OnBeginPlayGameplayLevel -= OnBeginPlayGameplayLevel;
    }
    
    private void OnBeginPlayGameplayLevel()
    {
        Debug.Assert(_connection.RequestedAreaId != null);

        var areaId = BGUFuncLibMap.GetCurLevelId(GameUtils.GetWorld());
        if (areaId > ushort.MaxValue)
        {
            throw new InvalidCastException("AreaId is greater than ushort max value");
        }
        _connection.JoinArea(new AreaId((ushort)areaId));
    }
    
    private void OnEndPlayGameplayLevel()
    {
        _connection.LeaveArea();
    }
}