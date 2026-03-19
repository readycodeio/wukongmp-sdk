using System.Threading.Tasks;
using UnrealEngine.Runtime;

namespace WukongMp.Sdk.Api;

public interface IWukongLocalApi
{
    /// Is the game currently in a gameplay level, as opposed to a menu or the like.
    bool IsGameplayLevel { get; }

    /// Shows a message on the player's screen.
    void ShowInfoMessage(string message);
    void ShowInfoMessage(string message, float timeoutSeconds);
    void HideInfoMessage();
    void AddChatMessage(string message, FLinearColor color);

    /// Waits for the given task to complete in a synchronous manner.
    void Wait(Task task);
}