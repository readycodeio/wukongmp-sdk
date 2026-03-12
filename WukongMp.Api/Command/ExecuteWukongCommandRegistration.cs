using ReadyM.Api.Command;
using UnrealEngine.Engine;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Command;

internal class ExecuteWukongCommandRegistration : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("command", ConsoleCommand.Create(ExecuteWukongConsoleCommand, isDebugOnly: true));
    }

    private void ExecuteWukongConsoleCommand(params string[] args)
    {
        var command = string.Join(" ", args);
        Logging.LogDebug("Executing command: {Command}", command);
        USystemLibrary.ExecuteConsoleCommand(GameUtils.GetWorld(), command, null);
    }
}