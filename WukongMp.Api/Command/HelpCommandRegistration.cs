using System.Linq;
using ReadyM.Api.Command;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;

namespace WukongMp.Api.Command;

internal class HelpCommandRegistration(WukongWidgetManager widgetManager) : IConsoleCommandRegistration
{
    private bool IsDebug
#if DEBUG
        => true;
#else
        => false;
#endif

    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("help", ConsoleCommand.Create(() => OnHelp(registry)));
    }

    private void OnHelp(ConsoleCommandRegistry registry)
    {
        var commands = registry.GetCommandNames(IsDebug).ToList();
        var formattedList = string.Join(", ", commands);
        var message = string.Format(BuiltinTexts.HelpCommandHeader, formattedList);
        widgetManager.AddMessageToConsole(message);
    }
}