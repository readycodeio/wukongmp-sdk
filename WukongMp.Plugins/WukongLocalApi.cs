using System;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Command;
using WukongMp.Api.UI;

namespace WukongMp.Plugins;

public class WukongLocalApi(
    WukongEventBus eventBus,
    WukongWidgetManager widgetManager,
    GameplayEventRouter eventRouter,
    WukongCommandConsole console)
{
    public bool IsGameplayLevel
        => eventBus.IsGameplayLevel;
    
    public void ShowInfoMessage(string message)
    {
        widgetManager.ShowInfoMessage(message);
    }

    public event ObstacleCollisionDelegate? OnObstacleCollision
    {
        add => eventRouter.OnObstacleCollision += value;
        remove => eventRouter.OnObstacleCollision -= value;
    }
    
    public event Action<AActor>? OnDisableObstacle
    {
        add => eventRouter.OnDisableObstacle += value;
        remove => eventRouter.OnDisableObstacle -= value;
    }

    public void WriteConsoleMessage(string message)
    {
        console.AddMessage(message);
    }

    public void ClearConsole()
    {
        console.Clear();
    }
}