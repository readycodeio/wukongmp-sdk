using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongWidgetApi(WukongWidgetManager widgetManager) : IWukongWidgetApi
{
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

    public void ShowTip(string tip, bool autoHide)
        => UiUtils.ShowTip(tip, autoHide);

    public void HideTip()
        => UiUtils.HideTip();

    public void SetCountdownVisibility(bool visible)
        => widgetManager.SetTimerVisibility(visible);

    public void SetCountdownText(int initialMinutes, int initialSeconds)
        => widgetManager.SetTimerText(initialMinutes, initialSeconds);
}