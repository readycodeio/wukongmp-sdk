using System.Collections.Generic;
using WukongMp.Api.Resources;

namespace WukongMp.Api.UI
{
    public class ChatWidget : GameWidgetBase
    {
        public static ChatWidget Instance { get; } = new();

        private ChatWidget() : base(Constants.ChatWidgetName) { }

        private int _messageId;

        private bool _levelLoaded;
        private Queue<string> _commandQueue = new();

        protected override void PostInitialize()
        {
            ClearMessages();
            ClearToolTipText();
            SetHelperText(Texts.ChatHelperDescription);
        }

        public bool HasFocus()
        {
            if (GameWidget == null)
            {
                return false;
            }

            return GameWidget.StopAction;
        }

        public void AddMessage(bool isServerMessage, string sender, string message)
        {
            if (GameWidget == null)
            {
                Logging.LogError("Could not add message. Chat widget not initialized");
                return;
            }

            Logging.LogDebug("Calling AddMessage function with message {Message} from {Sender}", message, sender);
            var cmd = $"AddMessage {isServerMessage} {++_messageId} {sender} {message}";
            if (!_levelLoaded)
            {
                _commandQueue.Enqueue(cmd);
            }
            else
            {
                GameWidget.CallFunctionByNameWithArguments(cmd, true);
            }
        }

        public override void SetVisibility(bool visible)
        {
            base.SetVisibility(visible);

            if (visible)
            {
                if (GameWidget == null)
                {
                    Logging.LogError("Could not add message. Chat widget not initialized");
                    return;
                }

                while (_commandQueue.TryDequeue(out var command))
                {
                    GameWidget.CallFunctionByNameWithArguments(command, true);
                }

                _levelLoaded = true;
            }
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

                ClearToolTipText();
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

        private void SetHelperText(string chatHelperText)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetHelperText {chatHelperText}", true);
        }

        private void ClearToolTipText()
        {
            GameWidget?.CallFunctionByNameWithArguments("GetSentMessage", true);
        }
    }
}