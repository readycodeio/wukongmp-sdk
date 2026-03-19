using ReadyM.Api.Command;
using WukongMp.Api.Command;

namespace WukongMp.Sdk.Api.Implementation;

/// API for the in-game console (F1).
internal sealed class WukongConsoleApi(
    WukongCommandConsole console,
    ConsoleCommandRegistry commandRegistry
) : IWukongConsoleApi
{
    public void AddCommands(params IConsoleCommandRegistration[] registrations)
    {
        commandRegistry.AddCommands(registrations);
    }

    public void WriteConsoleMessage(string message)
    {
        console.AddMessage(message);
    }
}