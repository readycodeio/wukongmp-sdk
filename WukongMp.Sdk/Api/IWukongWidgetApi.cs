using UnrealEngine.Runtime;

namespace WukongMp.Sdk.Api;

public interface IWukongWidgetApi
{
    void AddChatMessage(string message, FLinearColor color);
    void ToggleCommandVisibility();
    void AddMessageToConsole(string message);
    void ShowInGameWidgets(bool isOnGameplayLevel);
    void ShowInfoMessage(string message);
    void HideInfoMessage();
    void ShowTip(string tip, bool autoHide);
    void HideTip();
    void SetTimerVisibility(bool visible);
    void SetTimerText(int initialMinutes, int initialSeconds);
}