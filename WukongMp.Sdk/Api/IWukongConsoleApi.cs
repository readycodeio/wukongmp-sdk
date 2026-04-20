using System.Collections.Generic;
using ReadyM.Api.Command;

namespace WukongMp.Sdk.Api;

/// <summary>
/// API for the in-game console (F1).
/// </summary>
public interface IWukongConsoleApi : IConsoleCommandRegistry
{
    /// <summary>
    /// Registers a command to the in-game console.
    /// The command will be available to all players in the session.
    /// </summary>
    /// <param name="commandName">The name of the command that has to be typed in the console to execute the command.</param>
    /// <param name="command">Command handler</param>
    /// <param name="availableFirstParams">If specified, the console will show these as suggestions for the first parameter of the command. This is useful for commands that take a fixed set of parameters, such as "spawn wolf_sentinel".</param>
    void AddCommand(string commandName, ConsoleCommand command, IEnumerable<string>? availableFirstParams = null);

    /// <summary>
    /// Checks if a command with the given name is already registered in the console.
    /// </summary>
    /// <param name="commandName">Name of the command to check.</param>
    /// <returns><c>true</c> if a command with the given name is registered, <c>false</c> otherwise.</returns>
    bool HasCommand(string commandName);

    /// <summary>
    /// Logs a message to the in-game console. This can be used to provide feedback to the player after executing a command, or to log important information that the player should see.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogMessage(string message);
}