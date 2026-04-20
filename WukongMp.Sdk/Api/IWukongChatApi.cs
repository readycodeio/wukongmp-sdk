using UnrealEngine.Runtime;

namespace WukongMp.Sdk.Api;

/// <summary>
/// API for interacting with the in-game chat.
/// </summary>
public interface IWukongChatApi
{
    /// <summary>
    /// Sends a message to the in-game chat, as if the player wrote it.
    /// </summary>
    /// <param name="message">The message to send.</param>
    void SendPlayerMessage(string message);

    /// <summary>
    /// Sends a message to the in-game chat without a player name attached to it.
    /// </summary>
    /// <param name="message">The message to send.</param>
    void SendServerMessage(string message);

    /// <summary>
    /// Adds a message to the in-game chat, visible only to the local player.
    /// </summary>
    /// <param name="message">The message to add to the chat.</param>
    /// <param name="color">The color of the message in the chat.</param>
    void ShowLocalMessage(string message, FLinearColor color);
}