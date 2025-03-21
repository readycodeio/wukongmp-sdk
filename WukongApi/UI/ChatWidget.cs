namespace WukongApi.UI
{
    public class ChatWidget : GameWidgetBase
    {
        public static ChatWidget Instance { get; } = new ChatWidget();

        private ChatWidget() : base(Constants.ChatWidgetName) { }

        private int _messageId;

        protected override void PostInitialize()
        {
            ClearMessages();
        }

        public bool HasFocus()
        {
            if (GameWidget == null)
            {
                return false;
            }

            return GameWidget.StopAction;
        }

        public int AddMessage(bool isServerMessage, string sender, string message)
        {
            if (GameWidget != null)
            {
                Logging.LogDebug("Calling AddMessage function with message {Message} from {Sender}", message, sender);
                GameWidget.CallFunctionByNameWithArguments($"AddMessage {isServerMessage} {++_messageId} {sender} {message}", true);
                return _messageId;
            }

            Logging.LogError("Could not add message. Chat widget not initialized");
            return -1;
        }

        public void RemoveMessage(int messageId)
        {
            GameWidget?.CallFunctionByNameWithArguments($"RemoveMessage {messageId}", true);
        }

        public string GetMessage()
        {
            if (GameWidget != null)
            {
                GameWidget.CallFunctionByNameWithArguments("GetSentMessage", true);
                var message = GameWidget.ToolTipText.ToString();
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
            GameWidget?.CallFunctionByNameWithArguments("ChangeVisibility", true);
        }

        public void ClearMessages()
        {
            if (GameWidget != null)
            {
                GameWidget.CallFunctionByNameWithArguments("ClearMessages", true);
                _messageId = 0;
            }
        }

        public void SetHistoryNext()
        {
            if (HasFocus())
            {
                GameWidget?.CallFunctionByNameWithArguments("SetHistoryNext", true);
            }
        }

        public void SetHistoryPrev()
        {
            if (HasFocus())
            {
                GameWidget?.CallFunctionByNameWithArguments("SetHistoryPrev", true);
            }
        }

        public void SetInputFocus()
        {
            GameWidget?.CallFunctionByNameWithArguments("SetInputFocus", true);
        }

        public string CommitMessage()
        {
            GameWidget?.CallFunctionByNameWithArguments("CommitMessage", true);
            return GetMessage();
        }
    }
}