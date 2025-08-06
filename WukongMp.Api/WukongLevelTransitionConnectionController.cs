using System;
using System.Diagnostics;
using WukongMp.Api.Old;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongLevelTransitionConnectionController : IDisposable
{
    private readonly WukongEventBus _eventBus;
    private readonly WukongConnectionManager _connection;
    private readonly WukongSynchronizer _synchronizer;

    public WukongLevelTransitionConnectionController(
        WukongEventBus eventBus,
        WukongConnectionManager connection,
        WukongSynchronizer synchronizer
    )
    {
        _eventBus = eventBus;
        _connection = connection;
        _synchronizer = synchronizer;
        
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
        Debug.Assert(!_connection.EnteredRoom);
        
        Logging.LogInformation("Initializing widgets");
        ModWidgetsUtils.SpawnWidgetManagerActor();
        ModWidgetsUtils.InitializeWidgets();

        _connection.EnterRoom();
    }
    
    private void OnEndPlayGameplayLevel()
    {
        _connection.ExitRoom();
        
        Logging.LogInformation("Deinitializing widgets");
        ModWidgetsUtils.DeinitializeWidgets();
    }
    
    private void OnLoadingScreenClose()
    {
        if (_connection.EnteredRoom)
        {
            ChatWidget.Instance.SetVisibility(true);
        }
    }
}