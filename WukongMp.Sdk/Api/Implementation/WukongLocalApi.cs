using System.Threading.Tasks;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Relay.Client;
using WukongMp.Api;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongLocalApi(
    WukongEventBus eventBus,
    WukongWidgetManager widgetManager,
    ReceiveSchedulerSystem schedulerSystem,
    ClientEcsUpdateLoop ecsLoop
) : IWukongLocalApi
{
    /// Is the game currently in a gameplay level, as opposed to a menu or the like.
    public bool IsGameplayLevel
        => eventBus.IsGameplayLevel;

    /// Shows a message on the player's screen, that persists until HideInfoMessage is called.
    public void ShowInfoMessage(string message)
    {
        widgetManager.ShowInfoMessage(message);
    }
    
    /// Shows a message on the player's screen.
    /// The message will automatically disappear after the given timeout.
    public void ShowInfoMessage(string message, float timeoutSeconds)
    {
        ShowInfoMessage(message);
        _ = Task.Run(async () =>
        {
            await Task.Delay((int)(timeoutSeconds * 1000));
            schedulerSystem.Scheduler.Schedule((_, wm) => { wm.HideInfoMessage(); }, widgetManager);
        });
    }

    public void HideInfoMessage() => widgetManager.HideInfoMessage();

    /// Waits for the given task to complete in a synchronous manner.
    public void Wait(Task task) => ecsLoop.Wait(task);

    public void ShowTip(string message, bool autoHide)
    {
        UiUtils.ShowTip(message, autoHide);
    }
}