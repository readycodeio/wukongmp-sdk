using System;
using System.Collections.Generic;
using ReadyM.Api.Command;

namespace WukongMp.Sdk.Api;

/// <summary>
/// API for the in-game console (F1).
/// </summary>
public interface IWukongConsoleApi
{
    /// <summary>
    /// Registers a console command that all players in the session can run.
    /// </summary>
    /// <param name="commandName">Text the player types to invoke the command (e.g. "spawn").</param>
    /// <param name="command">Handler that runs when the command is executed.</param>
    /// <param name="availableFirstParams">
    /// Optional fixed list of autocomplete suggestions for the command's first parameter.
    /// The list is captured once at registration, so use this overload when the
    /// suggestions never change, e.g. a known set of spawnables: "spawn wolf_sentinel".
    /// </param>
    void AddCommand(string commandName, ConsoleCommand command, IEnumerable<string>? availableFirstParams = null);

    /// <summary>
    /// Registers a console command that all players in the session can run.
    /// </summary>
    /// <param name="commandName">Text the player types to invoke the command (e.g. "kick").</param>
    /// <param name="command">Handler that runs when the command is executed.</param>
    /// <param name="availableFirstParams">
    /// A factory that produces autocomplete suggestions for the command's first
    /// parameter. It is invoked each time the console requests suggestions, so use this
    /// overload when the values depend on live state, e.g. currently connected players
    /// or spawned entities.
    /// </param>
    void AddCommand(string commandName, ConsoleCommand command, Func<IEnumerable<string>> availableFirstParams);

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