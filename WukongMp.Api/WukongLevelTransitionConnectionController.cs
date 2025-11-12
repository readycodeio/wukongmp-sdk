using b1;
using ReadyM.Api.Multiplayer.Idents;
using System;
using System.Diagnostics;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongLevelTransitionConnectionController : IDisposable
{
    private readonly WukongEventBus _eventBus;
    private readonly WukongConnectionManager _connection;
    private readonly WukongWidgetManager _widgetManager;

    public WukongLevelTransitionConnectionController(
        WukongEventBus eventBus,
        WukongConnectionManager connection,
        WukongWidgetManager widgetManager
    )
    {
        _eventBus = eventBus;
        _connection = connection;
        _widgetManager = widgetManager;
        
        _eventBus.OnBeginPlayGameplayLevel += OnBeginPlayGameplayLevel;
        _eventBus.OnEndPlayGameplayLevel += OnEndPlayGameplayLevel;
        _eventBus.OnLoadingScreenClose += OnLoadingScreenClose;
    }
    
    public void Dispose()
    {
        _eventBus.OnLoadingScreenClose -= OnLoadingScreenClose;
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
    
    private void OnLoadingScreenClose()
    {
        if (_connection.RequestedAreaId != null)
        {
            _widgetManager.ShowInGameWidgets();
        }
    }
}