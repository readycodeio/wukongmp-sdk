using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReadyM.Api.Command;
using ReadyM.Relay.Client;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Command;
using WukongMp.Api.UI;

namespace WukongMp.Sdk.Api;

public class WukongLocalApi(
    WukongEventBus eventBus, 
    WukongWidgetManager widgetManager, 
    WukongCommandConsole console,
    GameplayEventRouter eventRouter, 
    ConsoleCommandRegistry commandRegistry, 
    IClientEcsUpdateLoop ecsUpdateLoop)
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

    public void AddCommands(IEnumerable<IConsoleCommandRegistration> registrations)
    {
        commandRegistry.AddCommands(registrations);
    }

    /// Waits for the given task to complete in a synchronous manner.
    public void Wait(Task task)
    {
        ecsUpdateLoop.Wait(task);
    }

    public void WriteConsoleMessage(string message)
    {
        console.AddMessage(message);
    }
}