using System.Threading.Tasks;

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
    /// Shows a tip message on the player's screen using the game's UI.
    /// </summary>
    /// <param name="message">The message to show.</param>
    /// <param name="autoHide">Whether the message should automatically hide after 5 seconds</param>
    void ShowTip(string message, bool autoHide);

    /// <summary>
    /// Waits for the given task to complete in a synchronous manner.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    void Wait(Task task);
}