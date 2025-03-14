namespace WukongApi.UI
{
    public class ChatWidget : GameWidgetBase
    {
        private int _messageId;

        public ChatWidget() : base(Constants.ChatWidgetName) { }

        protected override void PostInitialize()
        {
            ClearMessages();
        }

        public int AddMessage(bool isServerMesssage, string sender, string message)
        {
            if (_gameWidget != null)
            {
                Logging.LogDebug("Calling AddMessage function with message {Message} from {Sender}", message, sender);
                _gameWidget.CallFunctionByNameWithArguments($"AddMessage {isServerMesssage} {++_messageId} {sender} {message}", true);
                return _messageId;
            }
            else
            {
                Logging.LogError("Could not add message. Chat widget not initialized");
                return -1;
            }
        }

        public void RemoveMessage(int messageId)
        {
            if (_gameWidget != null)
            {
                _gameWidget.CallFunctionByNameWithArguments($"RemoveMessage {messageId}", true);
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
                    Logging.LogDebug("Got message: {Message} in GetSentMessage function", message);
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

        public void ClearMessages()
        {
            if (_gameWidget != null)
            {
                _gameWidget.CallFunctionByNameWithArguments("ClearMessages", true);
                _messageId = 0;
            }
        }
    }
}
