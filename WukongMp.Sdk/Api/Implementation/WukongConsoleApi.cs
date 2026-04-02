using System.Collections.Generic;
using ReadyM.Api.Command;
using WukongMp.Api.Command;

namespace WukongMp.Sdk.Api.Implementation;

/// API for the in-game console (F1).
internal sealed class WukongConsoleApi(
    WukongCommandConsole console,
    ConsoleCommandRegistry commandRegistry
) : IWukongConsoleApi
{
    public void AddCommand(string commandName, ConsoleCommand command, IEnumerable<string>? availableFirstParams = null)
    {
        commandRegistry.AddCommand(commandName, command, availableFirstParams);
    }

    public bool HasCommand(string commandName)
    {
        return commandRegistry.HasCommand(commandName);
    }

    public void WriteConsoleMessage(string message)
    {
        console.AddMessage(message);
    }
}