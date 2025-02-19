using UnrealEngine.UMG;

namespace WukongApi.UI
{
    public class ChatWidget
    {
        private UUserWidget _chatWidget;

        public void AddMessage(bool isServerMesssage, string sender, string message)
        {
            if (_chatWidget != null)
            {
                Logging.LogDebug($"Calling AddMessage function with message {message} from {sender}");
                _chatWidget.CallFunctionByNameWithArguments($"AddMessage {isServerMesssage} {sender} {message}", true);
            }
            else
            {
                Logging.LogError("Chat widget not initialized");
            }
        }

        public string GetMessage()
        {
            if (_chatWidget != null)
            {
                _chatWidget.CallFunctionByNameWithArguments("GetSentMessage", true);
                var message = _chatWidget.ToolTipText.ToString();
                if (message.Length > 0)
                {
                    Logging.LogDebug($"Got message: {message} in GetSentMessage function");
                }

                return message;
            }

            return "";
        }

        public void Initialize()
        {
            _chatWidget = BlueprintUIUtils.GetWidget(Constants.ChatWidgetName);
            if (_chatWidget != null)
            {
                Logging.LogDebug("Chat widget initialized!.");
            }
            else
            {
                Logging.LogError("Cannot initialize chat widget");
            }
        }

        public void ToggleVisibility()
        {
            if (_chatWidget != null)
            {
                _chatWidget.CallFunctionByNameWithArguments("ChangeVisibility", true);
            }
        }
    }
}
