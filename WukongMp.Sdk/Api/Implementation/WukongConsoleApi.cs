using System;
using System.Collections.Generic;
using System.Linq;
using ReadyM.Api.Command;
using WukongMp.Api.Command;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongConsoleApi(
    WukongCommandConsole console,
    ConsoleCommandRegistry commandRegistry
) : IWukongConsoleApi
{
    public void AddCommand(string commandName, ConsoleCommand command, IEnumerable<string>? availableFirstParams = null)
    {
        Func<IEnumerable<string>>? factory = null;

        if (availableFirstParams != null)
        {
            var cached = availableFirstParams.ToList();
            factory = () => cached;
        }

        commandRegistry.AddCommand(commandName, command, factory);
    }

    public void AddCommand(string commandName, ConsoleCommand command, Func<IEnumerable<string>> availableFirstParams)
    {
        commandRegistry.AddCommand(commandName, command, availableFirstParams);
    }

    public bool HasCommand(string commandName)
    {
        return commandRegistry.HasCommand(commandName);
    }

    public void LogMessage(string message)
    {
        console.AddMessage(message);
    }
}