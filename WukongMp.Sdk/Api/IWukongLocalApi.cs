using System.Threading.Tasks;
using UnrealEngine.Runtime;

namespace WukongMp.Sdk.Api;

public interface IWukongLocalApi
{
    /// <summary>
    /// Is the game currently in a gameplay level, as opposed to a menu or the like?
    /// </summary>
    bool IsGameplayLevel { get; }

    /// <summary>
    /// Shows a message on the player's screen.
    /// </summary>
    /// <param name="message">The message to show.</param>
    void ShowInfoMessage(string message);

    /// <summary>
    /// Shows a message on the player's screen for a certain amount of time.
    /// </summary>
    /// <param name="message">The message to show.</param>
    /// <param name="timeoutSeconds">The amount of time, in seconds, to show the message for.</param>
    void ShowInfoMessage(string message, float timeoutSeconds);

    /// <summary>
    /// Hides the message currently being shown on the player's screen, if any.
    /// </summary>
    void HideInfoMessage();

    /// <summary>
    /// Adds a message to the in-game chat, visible only to the local player.
    /// </summary>
    /// <param name="message">The message to add to the chat.</param>
    /// <param name="color">The color of the message in the chat.</param>
    void AddChatMessage(string message, FLinearColor color);

    /// <summary>
    /// Waits for the given task to complete in a synchronous manner.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    void Wait(Task task);
}