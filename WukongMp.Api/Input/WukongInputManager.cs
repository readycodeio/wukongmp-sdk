using WukongMp.Api.Chat;
using WukongMp.Api.Command;
using WukongMp.Api.UI;

namespace WukongMp.Api.Input;

internal sealed class WukongInputManager(
    WukongCommandConsole commandConsole,
    WukongChatter chatter,
    WukongWidgetManager widgetManager)
{
    public void HandleEnterPressed()
    {
        if (widgetManager.IsCommandVisible())
        {
            if (!widgetManager.CommandHasFocus())
            {
                widgetManager.SetCommandInputFocus();
            }
            else
            {
                var command = widgetManager.CommitCommand();
                commandConsole.ProcessCommand(command);
            }
        }
        else
        {
            if (!widgetManager.ChatHasFocus)
            {
                widgetManager.SetChatInputFocus();
            }
            else
            {
                var message = widgetManager.CommitChatMessage();
                chatter.ProcessMessage(message);
            }
        }
    }

    public bool CanApplyInput()
    {
        return !widgetManager.ChatHasFocus && !widgetManager.CommandHasFocus();
    }
}
