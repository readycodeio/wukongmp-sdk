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
}