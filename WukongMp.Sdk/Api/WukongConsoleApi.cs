using System.Collections.Generic;
using ReadyM.Api.Command;
using WukongMp.Api.Command;

namespace WukongMp.Sdk.Api;

/// API for the in-game console (F1).
public sealed class WukongConsoleApi
{
    private readonly WukongCommandConsole console;
    private readonly ConsoleCommandRegistry commandRegistry;
    
    internal WukongConsoleApi(WukongCommandConsole console, ConsoleCommandRegistry commandRegistry)
    {
        this.console = console;
        this.commandRegistry = commandRegistry;
    }
    
    public void AddCommands(IEnumerable<IConsoleCommandRegistration> registrations)
    {
        commandRegistry.AddCommands(registrations);
    }

    public void WriteConsoleMessage(string message)
    {
        console.AddMessage(message);
    }

}