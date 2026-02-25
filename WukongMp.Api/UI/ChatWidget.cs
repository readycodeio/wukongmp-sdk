using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnrealEngine.Runtime;
using WukongMp.Api.Compat;
using WukongMp.Api.Resources;

namespace WukongMp.Api.UI
{
    public class ChatWidget : GameWidgetBase
    {
        private struct MessageEntry
        {
            public bool ShowSender;
            public int MessageId;
            public string Sender;
            public string Message;
            public FLinearColor Color;
        }

        private const string ChatWidgetPath = "/Game/Mods/WukongMod/WBP_MultiplayerChat.WBP_MultiplayerChat_C";

        public ChatWidget() : base(ChatWidgetPath) { }

        private int _messageId;

        private bool _levelLoaded;
        private readonly Queue<string> _commandQueue = new();
        private readonly Queue<MessageEntry> _messageQueue = new();

        private bool _hiddenManually;

        protected override void PostInitialize()
        {
            InitNativeFunctions();
            ClearMessages();
            ClearToolTipText();
            SetHelperText(Texts.ChatHelperNoSendDescription);
            SetWritable(false);
        }

        public void SetWritingEnabled(bool enabled)
        {
            SetHelperText(enabled ? Texts.ChatHelperDescription : Texts.ChatHelperNoSendDescription);
            SetWritable(enabled);
        }

        public bool HasFocus()
        {
            if (GameWidget == null)
            {
                return false;
            }

            return GameWidget.StopAction;
        }

        private void SetWritable(bool isWritable)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetWritable {isWritable}", true);
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

        public unsafe void AddMessageWithColor(bool showSender, string senderName, string message, FLinearColor messageColor)
        {
            if (GameWidget == null ||
                AddMessageWithColor_ShowSender_PropertyAddress == null ||
                AddMessageWithColor_MessageId_PropertyAddress == null ||
                AddMessageWithColor_User_PropertyAddress == null ||
                AddMessageWithColor_Message_PropertyAddress == null ||
                AddMessageWithColor_MessageColor_PropertyAddress == null)
            {
                Logging.LogError("GameWidget or property address is null in WBP_MultiplayerChat_C:AddMessageWithColor.");
                return;
            }

            if (!AddMessageWithColor_IsValid)
            {
                Logging.LogError("Function WBP_MultiplayerChat_C:AddMessageWithColor is not valid.");
                return;
            }

            Logging.LogDebug("Calling AddMessage function with message {Message} from {Sender}", message, senderName);

            var messageId = ++_messageId;
            if (!_levelLoaded)
            {
                _messageQueue.Enqueue(new MessageEntry { Color = messageColor, Message = message, Sender = senderName, MessageId = messageId, ShowSender = showSender});
            }
            else
            {
                byte* ptr = stackalloc byte[(int)(uint)(AddMessageWithColor_ParamsSize + 16)];
                int num = (int)((16L - (long)ptr) & 0xF);
                byte* ptr2 = ptr + num;
                Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)AddMessageWithColor_ParamsSize);
                IntPtr intPtr = new IntPtr(ptr2);

                BoolMarshaler.ToNative(IntPtr.Add(intPtr, AddMessageWithColor_ShowSender_Offset), 0, AddMessageWithColor_ShowSender_PropertyAddress.Address, showSender);
                BlittableTypeMarshaler<int>.ToNative(IntPtr.Add(intPtr, AddMessageWithColor_MessageId_Offset), 0, AddMessageWithColor_MessageId_PropertyAddress.Address, messageId);
                FStringMarshaler.ToNative(IntPtr.Add(intPtr, AddMessageWithColor_User_Offset), 0, AddMessageWithColor_User_PropertyAddress.Address, senderName);
                FStringMarshaler.ToNative(IntPtr.Add(intPtr, AddMessageWithColor_Message_Offset), 0, AddMessageWithColor_Message_PropertyAddress.Address, message);
                BlittableTypeMarshaler<FLinearColor>.ToNative(IntPtr.Add(intPtr, AddMessageWithColor_MessageColor_Offset), 0, AddMessageWithColor_MessageColor_PropertyAddress.Address, messageColor);

                NativeReflection.InvokeFunctionOptimized(GameWidget.Address, AddMessageWithColor_FunctionAddress, intPtr, AddMessageWithColor_ParamsSize);

                NativeReflection.DestroyValue_InContainer(AddMessageWithColor_User_PropertyAddress.Address, intPtr);
                NativeReflection.DestroyValue_InContainer(AddMessageWithColor_Message_PropertyAddress.Address, intPtr);
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
                while (_messageQueue.TryDequeue(out var message))
                {
                    AddMessageWithColor(message.ShowSender, message.Sender, message.Message, message.Color);
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
            Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)IsChatVisible_ParamsSize);
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

        // AddMessageWithColor function
        private static bool AddMessageWithColor_IsValid;
        private static IntPtr AddMessageWithColor_FunctionAddress;
        private static int AddMessageWithColor_ParamsSize;

        private static bool AddMessageWithColor_ShowSender_IsValid;
        private static FFieldAddress? AddMessageWithColor_ShowSender_PropertyAddress;
        private static int AddMessageWithColor_ShowSender_Offset;
        private static bool AddMessageWithColor_MessageId_IsValid;
        private static FFieldAddress? AddMessageWithColor_MessageId_PropertyAddress;
        private static int AddMessageWithColor_MessageId_Offset;
        private static bool AddMessageWithColor_User_IsValid;
        private static FFieldAddress? AddMessageWithColor_User_PropertyAddress;
        private static int AddMessageWithColor_User_Offset;
        private static bool AddMessageWithColor_Message_IsValid;
        private static FFieldAddress? AddMessageWithColor_Message_PropertyAddress;
        private static int AddMessageWithColor_Message_Offset;
        private static bool AddMessageWithColor_MessageColor_IsValid;
        private static FFieldAddress? AddMessageWithColor_MessageColor_PropertyAddress;
        private static int AddMessageWithColor_MessageColor_Offset;

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

            AddMessageWithColor_FunctionAddress = NativeReflectionCached.GetFunction(@class, "AddMessageWithColor");
            AddMessageWithColor_ParamsSize = NativeReflection.GetFunctionParamsSize(AddMessageWithColor_FunctionAddress);
            NativeReflectionCached.GetPropertyRef(ref AddMessageWithColor_ShowSender_PropertyAddress, AddMessageWithColor_FunctionAddress, "ShowSender");
            AddMessageWithColor_ShowSender_Offset = NativeReflectionCached.GetPropertyOffset(AddMessageWithColor_FunctionAddress, "ShowSender");
            AddMessageWithColor_ShowSender_IsValid = NativeReflectionCached.ValidatePropertyClass(AddMessageWithColor_FunctionAddress, "ShowSender", Classes.FBoolProperty);
            NativeReflectionCached.GetPropertyRef(ref AddMessageWithColor_MessageId_PropertyAddress, AddMessageWithColor_FunctionAddress, "MessageId");
            AddMessageWithColor_MessageId_Offset = NativeReflectionCached.GetPropertyOffset(AddMessageWithColor_FunctionAddress, "MessageId");
            AddMessageWithColor_MessageId_IsValid = NativeReflectionCached.ValidatePropertyClass(AddMessageWithColor_FunctionAddress, "MessageId", Classes.FIntProperty);
            NativeReflectionCached.GetPropertyRef(ref AddMessageWithColor_User_PropertyAddress, AddMessageWithColor_FunctionAddress, "User");
            AddMessageWithColor_User_Offset = NativeReflectionCached.GetPropertyOffset(AddMessageWithColor_FunctionAddress, "User");
            AddMessageWithColor_User_IsValid = NativeReflectionCached.ValidatePropertyClass(AddMessageWithColor_FunctionAddress, "User", Classes.FStrProperty);
            NativeReflectionCached.GetPropertyRef(ref AddMessageWithColor_Message_PropertyAddress, AddMessageWithColor_FunctionAddress, "Message");
            AddMessageWithColor_Message_Offset = NativeReflectionCached.GetPropertyOffset(AddMessageWithColor_FunctionAddress, "Message");
            AddMessageWithColor_Message_IsValid = NativeReflectionCached.ValidatePropertyClass(AddMessageWithColor_FunctionAddress, "Message", Classes.FStrProperty);
            NativeReflectionCached.GetPropertyRef(ref AddMessageWithColor_MessageColor_PropertyAddress, AddMessageWithColor_FunctionAddress, "MessageColor");
            AddMessageWithColor_MessageColor_Offset = NativeReflectionCached.GetPropertyOffset(AddMessageWithColor_FunctionAddress, "MessageColor");
            AddMessageWithColor_MessageColor_IsValid = NativeReflectionCached.ValidatePropertyClass(AddMessageWithColor_FunctionAddress, "MessageColor", Classes.FStructProperty);
            AddMessageWithColor_IsValid = AddMessageWithColor_FunctionAddress != IntPtr.Zero && AddMessageWithColor_ShowSender_IsValid && AddMessageWithColor_MessageId_IsValid && AddMessageWithColor_User_IsValid && AddMessageWithColor_Message_IsValid && AddMessageWithColor_MessageColor_IsValid;
            if (!AddMessageWithColor_IsValid)
                Logging.LogError("Function WBP_MultiplayerChat_C:AddMessageWithColor is not valid.");
        }
    }
}