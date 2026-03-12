using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReadyM.Api.Command;
using ReadyM.Relay.Client;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Command;
using WukongMp.Api.UI;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

public class WukongLocalApi
{
    private readonly WukongEventBus eventBus;
    private readonly WukongWidgetManager widgetManager;
    private readonly WukongCommandConsole console;
    private readonly GameplayEventRouter eventRouter;
    private readonly ConsoleCommandRegistry commandRegistry;
    private readonly IClientEcsUpdateLoop ecsUpdateLoop;

    internal WukongLocalApi(WukongEventBus eventBus,
        WukongWidgetManager widgetManager,
        WukongCommandConsole console,
        GameplayEventRouter eventRouter,
        ConsoleCommandRegistry commandRegistry,
        IClientEcsUpdateLoop ecsUpdateLoop)
    {
        this.eventBus = eventBus;
        this.widgetManager = widgetManager;
        this.console = console;
        this.eventRouter = eventRouter;
        this.commandRegistry = commandRegistry;
        this.ecsUpdateLoop = ecsUpdateLoop;
    }

    public bool IsGameplayLevel
        => eventBus.IsGameplayLevel;

    public void ShowInfoMessage(string message)
    {
        widgetManager.ShowInfoMessage(message);
    }

    public delegate void ObstacleCollisionDelegate(ReadyMainCharacter mainEntity, AActor obstacle, out bool shouldBlock);

    private Dictionary<ObstacleCollisionDelegate, InternalObstacleCollisionDelegate> obstacleCollisionDelegates = new();

    public event ObstacleCollisionDelegate? OnObstacleCollision
    {
        add
        {
            if (value is null) return;
            var del = new InternalObstacleCollisionDelegate(((entity, obstacle, out block) => { value.Invoke(new ReadyMainCharacter(ReadyM.Client, entity), obstacle, out block); }));
            obstacleCollisionDelegates[value] = del;
            eventRouter.OnObstacleCollision += del;
        }
        remove
        {
            if (value is null) return;
            if (obstacleCollisionDelegates.TryGetValue(value, out var del))
            {
                eventRouter.OnObstacleCollision -= del;
                obstacleCollisionDelegates.Remove(value);
            }
        }
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