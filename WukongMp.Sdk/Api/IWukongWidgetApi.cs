namespace WukongMp.Sdk.Api;

/// <summary>
/// Provides methods related to in-game widgets, such as chat messages, info messages, tips, and timers.
/// </summary>
public interface IWukongWidgetApi
{
    /// <summary>
    /// Toggles the visibility of the command console.
    /// </summary>
    void ToggleCommandVisibility();

    /// <summary>
    /// Adds a message to the in-game console.
    /// </summary>
    /// <param name="message">The message to add to the console.</param>
    void AddMessageToConsole(string message);

    /// <summary>
    /// Shows or hides in-game widgets based on whether the player is on the gameplay level.
    /// </summary>
    /// <param name="isOnGameplayLevel">Indicates whether the player is on a gameplay level.</param>
    void ShowInGameWidgets(bool isOnGameplayLevel);

    /// <summary>
    /// Displays an informational message on the screen.
    /// </summary>
    /// <param name="message">The informational message to display.</param>
    void ShowInfoMessage(string message);

    /// <summary>
    /// Hides the currently displayed informational message.
    /// </summary>
    void HideInfoMessage();

    /// <summary>
    /// Displays a tip on the screen using the game UI tip widget.
    /// </summary>
    /// <param name="tip">The tip to display.</param>
    /// <param name="autoHide">Indicates whether the tip should automatically hide after a duration.</param>
    void ShowTip(string tip, bool autoHide);

    /// <summary>
    /// Hides the currently displayed tip.
    /// </summary>
    void HideTip();

    /// <summary>
    /// Sets the visibility of the countdown timer widget.
    /// </summary>
    /// <param name="visible">Indicates whether the countdown timer should be visible.</param>
    void SetCountdownVisibility(bool visible);

    /// <summary>
    /// Sets the value of the countdown timer widget.
    /// </summary>
    /// <param name="initialMinutes">The initial minutes to display on the countdown timer.</param>
    /// <param name="initialSeconds">The initial seconds to display on the countdown timer.</param>
    void SetCountdownText(int initialMinutes, int initialSeconds);
}