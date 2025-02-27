namespace WukongApi.UI
{
    public class ChatWidget : GameWidgetBase
    {
        public ChatWidget() : base(Constants.ChatWidgetName) { }

        public void AddMessage(bool isServerMesssage, string sender, string message)
        {
            if (_gameWidget != null)
            {
                Logging.LogDebug($"Calling AddMessage function with message {message} from {sender}");
                _gameWidget.CallFunctionByNameWithArguments($"AddMessage {isServerMesssage} {sender} {message}", true);
            }
            else
            {
                Logging.LogError("Chat widget not initialized");
            }
        }

        public string GetMessage()
        {
            if (_gameWidget != null)
            {
                _gameWidget.CallFunctionByNameWithArguments("GetSentMessage", true);
                var message = _gameWidget.ToolTipText.ToString();
                if (message.Length > 0)
                {
                    Logging.LogDebug($"Got message: {message} in GetSentMessage function");
                }

                return message;
            }

            return "";
        }

        public void ToggleVisibility()
        {
            if (_gameWidget != null)
            {
                _gameWidget.CallFunctionByNameWithArguments("ChangeVisibility", true);
            }
        }
    }
}
