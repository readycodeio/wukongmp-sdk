using UnrealEngine.Runtime;
using WukongMp.Api.UI;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongWidgetApi(WukongWidgetManager widgetManager) : IWukongWidgetApi
{
    public void AddChatMessage(string message, FLinearColor color)
        => widgetManager.AddSystemChatMessage(message, color);

    public void ToggleCommandVisibility()
        => widgetManager.ToggleChatVisibility();

    public void AddMessageToConsole(string message)
        => widgetManager.AddMessageToConsole(message);

    public void ShowInGameWidgets(bool isOnGameplayLevel)
        => widgetManager.ShowInGameWidgets(isOnGameplayLevel);

    public void ShowInfoMessage(string message)
        => widgetManager.ShowInfoMessage(message);

    public void HideInfoMessage()
        => widgetManager.HideInfoMessage();
}