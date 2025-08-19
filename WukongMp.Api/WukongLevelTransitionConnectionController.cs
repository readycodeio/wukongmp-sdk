using System;
using System.Diagnostics;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongLevelTransitionConnectionController : IDisposable
{
    private readonly WukongEventBus _eventBus;
    private readonly WukongConnectionManager _connection;
    private readonly WukongSynchronizer _synchronizer;
    private readonly WukongWidgetManager _widgetManager;

    public WukongLevelTransitionConnectionController(
        WukongEventBus eventBus,
        WukongConnectionManager connection,
        WukongSynchronizer synchronizer,
        WukongWidgetManager widgetManager
    )
    {
        _eventBus = eventBus;
        _connection = connection;
        _synchronizer = synchronizer;
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
        
        Logging.LogInformation("Initializing widgets");
        ModWidgetsUtils.SpawnWidgetManagerActor();
        ModWidgetsUtils.InitializeWidgets();

        _connection.JoinArea(Constants.MainArea);
    }
    
    private void OnEndPlayGameplayLevel()
    {
        _connection.LeaveArea();
        
        Logging.LogInformation("Deinitializing widgets");
        ModWidgetsUtils.DeinitializeWidgets();
    }
    
    private void OnLoadingScreenClose()
    {
        if (_connection.RequestedAreaId != null)
        {
            _widgetManager.ShowInGameWidgets();
        }
    }
}