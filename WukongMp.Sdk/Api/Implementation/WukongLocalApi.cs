using System.Threading.Tasks;
using ReadyM.Relay.Client;
using WukongMp.Api;
using WukongMp.Api.UI;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongLocalApi(
    WukongEventBus eventBus,
    WukongWidgetManager widgetManager,
    IClientEcsUpdateLoop ecsUpdateLoop
) : IWukongLocalApi
{
    /// Is the game currently in a gameplay level, as opposed to a menu or the like.
    public bool IsGameplayLevel
        => eventBus.IsGameplayLevel;

    /// Shows a message on the player's screen.
    public void ShowInfoMessage(string message)
    {
        widgetManager.ShowInfoMessage(message);
    }

    /// Waits for the given task to complete in a synchronous manner.
    public void Wait(Task task)
    {
        ecsUpdateLoop.Wait(task);
    }
}