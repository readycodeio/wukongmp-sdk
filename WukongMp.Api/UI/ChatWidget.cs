using System;
using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongMp.Api.Compat;
using WukongMp.Api.Resources;

namespace WukongMp.Api.UI
{
    public class ChatWidget : GameWidgetBase
    {
        private const string ChatWidgetPath = "/Game/Mods/WukongMod/WBP_MultiplayerChat.WBP_MultiplayerChat_C";

        public ChatWidget() : base(ChatWidgetPath) { }

        private int _messageId;

        private bool _levelLoaded;
        private readonly Queue<string> _commandQueue = new();

        private bool _hiddenManually;

        protected override void PostInitialize()
        {
            InitNativeFunctions();
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
            }

            _levelLoaded = true;
        }

        public void ShowIfNotHidden()
        {
            if (!_hiddenManually)
            {
                SetVisibility(true);
            }
            else
            {
                SetVisibility(false);
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
            if (IsVisible())
            {
                _hiddenManually = true;
                SetVisibility(false);
            }
            else
            {
                _hiddenManually = false;
                SetVisibility(true);
            }
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

        public unsafe bool IsVisible()
        {
            if (GameWidget == null || IsChatVisible_ReturnValue_PropertyAddress == null)
            {
                Logging.LogError("GameWidget or property address is null in WBP_MultiplayerChat_C:IsChatVisible.");
                return false;
            }

            if (!IsChatVisible_IsValid)
            {
                Logging.LogError("Function WBP_MultiplayerChat_C:IsChatVisible is not valid.");
                return false;
            }

            byte* ptr = stackalloc byte[(int)(uint)(IsChatVisible_ParamsSize + 16)];
            int num = (int)((16L - (long)ptr) & 0xF);
            byte* ptr2 = ptr + num;
            System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)IsChatVisible_ParamsSize);
            IntPtr intPtr = new IntPtr(ptr2);

            NativeReflection.InvokeFunctionOptimized(GameWidget.Address, IsChatVisible_FunctionAddress, intPtr, IsChatVisible_ParamsSize);
            return BlittableTypeMarshaler<bool>.FromNative(IntPtr.Add(intPtr, IsChatVisible_ReturnValue_Offset), 0, IsChatVisible_ReturnValue_PropertyAddress.Address);
        }

        static ChatWidget()
        {
            InitNativeFunctions();
        }

        private static bool IsChatVisible_IsValid;
        private static IntPtr IsChatVisible_FunctionAddress;
        private static int IsChatVisible_ParamsSize;

        private static bool IsChatVisible_ReturnValue_IsValid;
        private static FFieldAddress? IsChatVisible_ReturnValue_PropertyAddress;
        private static int IsChatVisible_ReturnValue_Offset;

        public static void InitNativeFunctions()
        {
            IntPtr @class = NativeReflection.GetClass(ChatWidgetPath);
            IsChatVisible_FunctionAddress = NativeReflectionCached.GetFunction(@class, "IsChatVisible");
            IsChatVisible_ParamsSize = NativeReflection.GetFunctionParamsSize(IsChatVisible_FunctionAddress);

            NativeReflectionCached.GetPropertyRef(ref IsChatVisible_ReturnValue_PropertyAddress, IsChatVisible_FunctionAddress, "IsVisible");
            IsChatVisible_ReturnValue_Offset = NativeReflectionCached.GetPropertyOffset(IsChatVisible_FunctionAddress, "IsVisible");
            IsChatVisible_ReturnValue_IsValid = NativeReflectionCached.ValidatePropertyClass(IsChatVisible_FunctionAddress, "IsVisible", Classes.FBoolProperty);
            IsChatVisible_IsValid = IsChatVisible_FunctionAddress != IntPtr.Zero && IsChatVisible_ReturnValue_IsValid;
            if (!IsChatVisible_IsValid)
                Logging.LogError("Function WBP_MultiplayerChat_C:IsChatVisible is not valid.");
        }
    }
}