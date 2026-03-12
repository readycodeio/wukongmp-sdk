using WukongMp.Api.Chat;
using WukongMp.Api.Command;
using WukongMp.Api.UI;

namespace WukongMp.Api.Input;

internal class WukongInputManager
{
    private readonly WukongCommandConsole _commandConsole;
    private readonly WukongChatter _chatter;
    private readonly WukongWidgetManager _widgetManager;

    public WukongInputManager(
        WukongCommandConsole commandConsole,
        WukongChatter chatter,
        WukongWidgetManager widgetManager
    )
    {
        _commandConsole = commandConsole;
        _chatter = chatter;
        _widgetManager = widgetManager;
    }

    public void HandleEnterPressed()
    {
        if (_widgetManager.IsCommandVisible())
        {
            if (!_widgetManager.CommandHasFocus())
            {
                _widgetManager.SetCommandInputFocus();
            }
            else
            {
                var command = _widgetManager.CommitCommand();
                _commandConsole.ProcessCommand(command);
            }
        }
        else
        {
            if (!_widgetManager.ChatHasFocus())
            {
                _widgetManager.SetChatInputFocus();
            }
            else
            {
                var message = _widgetManager.CommitChatMessage();
                _chatter.ProcessMessage(message);
            }
        }
    }

    public bool CanApplyInput()
    {
        return !_widgetManager.ChatHasFocus() && !_widgetManager.CommandHasFocus();
    }
}
