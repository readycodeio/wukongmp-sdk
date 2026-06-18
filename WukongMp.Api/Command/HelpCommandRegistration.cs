using System.Linq;
using ReadyM.Api.Command;
using WukongMp.Api.Resources;

namespace WukongMp.Api.Command;

internal class HelpCommandRegistration(WukongCommandConsole console) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("help", ConsoleCommand.Create(OnHelp));
    }

    private void OnHelp()
    {
        var commands = console.GetAvailableCommands();
        var formattedList = string.Join("\n", commands.Select(c => $"  - {c}"));
        var message = string.Format(BuiltinTexts.HelpCommandHeader, commands.Count, formattedList);
        console.AddMessage(message);
    }
}