using System;
using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongMp.Api.Resources;

namespace WukongMp.Api.UI;

public class CommandConsoleWidget : GameWidgetBase
{
    private const string CommandConsoleWidgetPath = "/Game/Mods/CoreMod/WBP_CommandConsole.WBP_CommandConsole_C";
    public CommandConsoleWidget() : base(CommandConsoleWidgetPath) { }

    private bool _hiddenManually;

    protected override void PostInitialize()
    {
        InitNativeFunctions();
        SetHelperText(Texts.ChatHelperDescription);
    }

    public override void SetVisibility(bool visible)
    {
        base.SetVisibility(visible);
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

    private void SetHelperText(string chatHelperText)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetHelperText {chatHelperText}", true);
    }

    public unsafe string CommitCommand()
    {
        if (GameWidget == null || CommitCommand_ReturnValue_PropertyAddress == null)
        {
            Logging.LogError("GameWidget or property address is null in WBP_CommandConsole_C:CommitCommand.");
            return "";
        }

        if (!CommitCommand_IsValid)
        {
            Logging.LogError("Function WBP_CommandConsole_C:CommitCommand is not valid.");
            return "";
        }

        byte* ptr = stackalloc byte[(int)(uint)(CommitCommand_ParamsSize + 16)];
        int num = (int)((16L - (long)ptr) & 0xF);
        byte* ptr2 = ptr + num;
        System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)CommitCommand_ParamsSize);
        IntPtr intPtr = new IntPtr(ptr2);

        NativeReflection.InvokeFunctionOptimized(GameWidget.Address, CommitCommand_FunctionAddress, intPtr, CommitCommand_ParamsSize);
        var result = FStringMarshaler.FromNative(IntPtr.Add(intPtr, CommitCommand_ReturnValue_Offset), 0, CommitCommand_ReturnValue_PropertyAddress.Address);
        NativeReflection.DestroyValue_InContainer(CommitCommand_ReturnValue_PropertyAddress.Address, intPtr);
        return result;
    }

    public unsafe void SetAvailableCommands(List<string> availableCommands)
    {
        if (GameWidget == null || SetAvailableCommands_AvailableCommands_PropertyAddress == null)
        {
            Logging.LogError("GameWidget or property address is null in WBP_CommandConsole_C:SetAvailableCommands.");
            return;
        }

        if (!SetAvailableCommands_IsValid)
        {
            Logging.LogError("Function WBP_CommandConsole_C:SetAvailableCommands is not valid.");
            return;
        }

        byte* ptr = stackalloc byte[(int)(uint)(SetAvailableCommands_ParamsSize + 16)];
        int num = (int)((16L - (long)ptr) & 0xF);
        byte* ptr2 = ptr + num;
        System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)SetAvailableCommands_ParamsSize);
        IntPtr intPtr = new IntPtr(ptr2);

        TArrayCopyMarshaler<string> readTeamArrayCopyMarshaler = new TArrayCopyMarshaler<string>(1, SetAvailableCommands_AvailableCommands_PropertyAddress, CachedMarshalingDelegates<string, FStringMarshaler>.FromNative, CachedMarshalingDelegates<string, FStringMarshaler>.ToNative);
        readTeamArrayCopyMarshaler.ToNative(IntPtr.Add(intPtr, SetAvailableCommands_AvailableCommands_Offset), availableCommands);

        NativeReflection.InvokeFunctionOptimized(GameWidget.Address, SetAvailableCommands_FunctionAddress, intPtr, SetAvailableCommands_ParamsSize);

        NativeReflection.DestroyValue_InContainer(SetAvailableCommands_AvailableCommands_PropertyAddress.Address, intPtr);
    }

    public unsafe void AddMessage(string message)
    {
        if (GameWidget == null || AddMessage_Message_PropertyAddress == null)
        {
            Logging.LogError("GameWidget or property address is null in WBP_CommandConsole_C:AddMessage.");
            return;
        }

        if (!AddMessage_IsValid)
        {
            Logging.LogError("Function WBP_CommandConsole_C:AddMessage is not valid.");
            return;
        }

        byte* ptr = stackalloc byte[(int)(uint)(AddMessage_ParamsSize + 16)];
        int num = (int)((16L - (long)ptr) & 0xF);
        byte* ptr2 = ptr + num;
        System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)AddMessage_ParamsSize);
        IntPtr intPtr = new IntPtr(ptr2);

        FStringMarshaler.ToNative(IntPtr.Add(intPtr, AddMessage_Message_Offset), 0, AddMessage_Message_PropertyAddress.Address, message);

        NativeReflection.InvokeFunctionOptimized(GameWidget.Address, AddMessage_FunctionAddress, intPtr, AddMessage_ParamsSize);

        NativeReflection.DestroyValue_InContainer(AddMessage_Message_PropertyAddress.Address, intPtr);
    }

    public unsafe bool IsVisible()
    {
        if (GameWidget == null || IsConsoleVisible_ReturnValue_PropertyAddress == null)
        {
            Logging.LogError("GameWidget or property address is null in WBP_CommandConsole_C:IsConsoleVisible.");
            return false;
        }

        if (!IsConsoleVisible_IsValid)
        {
            Logging.LogError("Function WBP_CommandConsole_C:IsConsoleVisible is not valid.");
            return false;
        }

        byte* ptr = stackalloc byte[(int)(uint)(IsConsoleVisible_ParamsSize + 16)];
        int num = (int)((16L - (long)ptr) & 0xF);
        byte* ptr2 = ptr + num;
        System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)IsConsoleVisible_ParamsSize);
        IntPtr intPtr = new IntPtr(ptr2);

        NativeReflection.InvokeFunctionOptimized(GameWidget.Address, IsConsoleVisible_FunctionAddress, intPtr, IsConsoleVisible_ParamsSize);
        return BlittableTypeMarshaler<bool>.FromNative(IntPtr.Add(intPtr, IsConsoleVisible_ReturnValue_Offset), 0, IsConsoleVisible_ReturnValue_PropertyAddress.Address);
    }

    public unsafe bool HasFocus()
    {
        if (GameWidget == null || HasFocus_ReturnValue_PropertyAddress == null)
        {
            Logging.LogError("GameWidget or property address is null in WBP_CommandConsole_C:GetHasFocus.");
            return false;
        }

        if (!HasFocus_IsValid)
        {
            Logging.LogError("Function WBP_CommandConsole_C:GetHasFocus is not valid.");
            return false;
        }

        byte* ptr = stackalloc byte[(int)(uint)(HasFocus_ParamsSize + 16)];
        int num = (int)((16L - (long)ptr) & 0xF);
        byte* ptr2 = ptr + num;
        System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)HasFocus_ParamsSize);
        IntPtr intPtr = new IntPtr(ptr2);

        NativeReflection.InvokeFunctionOptimized(GameWidget.Address, HasFocus_FunctionAddress, intPtr, HasFocus_ParamsSize);
        return BlittableTypeMarshaler<bool>.FromNative(IntPtr.Add(intPtr, HasFocus_ReturnValue_Offset), 0, HasFocus_ReturnValue_PropertyAddress.Address);
    }

    static CommandConsoleWidget()
    {
        InitNativeFunctions();
    }

    // IsVisible function
    private static bool IsConsoleVisible_IsValid;
    private static IntPtr IsConsoleVisible_FunctionAddress;
    private static int IsConsoleVisible_ParamsSize;

    private static bool IsConsoleVisible_ReturnValue_IsValid;
    private static FFieldAddress? IsConsoleVisible_ReturnValue_PropertyAddress;
    private static int IsConsoleVisible_ReturnValue_Offset;

    // HasFocus function
    private static bool HasFocus_IsValid;
    private static IntPtr HasFocus_FunctionAddress;
    private static int HasFocus_ParamsSize;

    private static bool HasFocus_ReturnValue_IsValid;
    private static FFieldAddress? HasFocus_ReturnValue_PropertyAddress;
    private static int HasFocus_ReturnValue_Offset;

    // CommitCommand function
    private static bool CommitCommand_IsValid;
    private static IntPtr CommitCommand_FunctionAddress;
    private static int CommitCommand_ParamsSize;

    private static bool CommitCommand_ReturnValue_IsValid;
    private static FFieldAddress? CommitCommand_ReturnValue_PropertyAddress;
    private static int CommitCommand_ReturnValue_Offset;

    // SetAvailableCommands function
    private static bool SetAvailableCommands_IsValid;
    private static IntPtr SetAvailableCommands_FunctionAddress;
    private static int SetAvailableCommands_ParamsSize;

    private static bool SetAvailableCommands_AvailableCommands_IsValid;
    private static FFieldAddress? SetAvailableCommands_AvailableCommands_PropertyAddress;
    private static int SetAvailableCommands_AvailableCommands_Offset;

    // AddMessage function
    private static bool AddMessage_IsValid;
    private static IntPtr AddMessage_FunctionAddress;
    private static int AddMessage_ParamsSize;

    private static bool AddMessage_Message_IsValid;
    private static FFieldAddress? AddMessage_Message_PropertyAddress;
    private static int AddMessage_Message_Offset;

    public static void InitNativeFunctions()
    {
        IntPtr @class = NativeReflection.GetClass(CommandConsoleWidgetPath);

        IsConsoleVisible_FunctionAddress = NativeReflectionCached.GetFunction(@class, "IsConsoleVisible");
        IsConsoleVisible_ParamsSize = NativeReflection.GetFunctionParamsSize(IsConsoleVisible_FunctionAddress);
        NativeReflectionCached.GetPropertyRef(ref IsConsoleVisible_ReturnValue_PropertyAddress, IsConsoleVisible_FunctionAddress, "IsVisible");
        IsConsoleVisible_ReturnValue_Offset = NativeReflectionCached.GetPropertyOffset(IsConsoleVisible_FunctionAddress, "IsVisible");
        IsConsoleVisible_ReturnValue_IsValid = NativeReflectionCached.ValidatePropertyClass(IsConsoleVisible_FunctionAddress, "IsVisible", Classes.FBoolProperty);
        IsConsoleVisible_IsValid = IsConsoleVisible_FunctionAddress != IntPtr.Zero && IsConsoleVisible_ReturnValue_IsValid;
        if (!IsConsoleVisible_IsValid)
            Logging.LogError("Function WBP_CommandConsole_C:IsConsoleVisible is not valid.");

        HasFocus_FunctionAddress = NativeReflectionCached.GetFunction(@class, "GetHasFocus");
        HasFocus_ParamsSize = NativeReflection.GetFunctionParamsSize(HasFocus_FunctionAddress);
        NativeReflectionCached.GetPropertyRef(ref HasFocus_ReturnValue_PropertyAddress, HasFocus_FunctionAddress, "HasFocus");
        HasFocus_ReturnValue_Offset = NativeReflectionCached.GetPropertyOffset(HasFocus_FunctionAddress, "HasFocus");
        HasFocus_ReturnValue_IsValid = NativeReflectionCached.ValidatePropertyClass(HasFocus_FunctionAddress, "HasFocus", Classes.FBoolProperty);
        HasFocus_IsValid = HasFocus_FunctionAddress != IntPtr.Zero && HasFocus_ReturnValue_IsValid;
        if (!HasFocus_IsValid)
            Logging.LogError("Function WBP_CommandConsole_C:GetHasFocus is not valid.");

        CommitCommand_FunctionAddress = NativeReflectionCached.GetFunction(@class, "CommitCommand");
        CommitCommand_ParamsSize = NativeReflection.GetFunctionParamsSize(CommitCommand_FunctionAddress);
        NativeReflectionCached.GetPropertyRef(ref CommitCommand_ReturnValue_PropertyAddress, CommitCommand_FunctionAddress, "Command");
        CommitCommand_ReturnValue_Offset = NativeReflectionCached.GetPropertyOffset(CommitCommand_FunctionAddress, "Command");
        CommitCommand_ReturnValue_IsValid = NativeReflectionCached.ValidatePropertyClass(CommitCommand_FunctionAddress, "Command", Classes.FStrProperty);
        CommitCommand_IsValid = CommitCommand_FunctionAddress != IntPtr.Zero && CommitCommand_ReturnValue_IsValid;
        if (!CommitCommand_IsValid)
            Logging.LogError("Function WBP_CommandConsole_C:CommitCommand is not valid.");

        SetAvailableCommands_FunctionAddress = NativeReflectionCached.GetFunction(@class, "SetAvailableCommands");
        SetAvailableCommands_ParamsSize = NativeReflection.GetFunctionParamsSize(SetAvailableCommands_FunctionAddress);
        NativeReflectionCached.GetPropertyRef(ref SetAvailableCommands_AvailableCommands_PropertyAddress, SetAvailableCommands_FunctionAddress, "AvailableCommands");
        SetAvailableCommands_AvailableCommands_Offset = NativeReflectionCached.GetPropertyOffset(SetAvailableCommands_FunctionAddress, "AvailableCommands");
        SetAvailableCommands_AvailableCommands_IsValid = NativeReflectionCached.ValidatePropertyClass(SetAvailableCommands_FunctionAddress, "AvailableCommands", Classes.FArrayProperty);
        SetAvailableCommands_IsValid = SetAvailableCommands_FunctionAddress != IntPtr.Zero && SetAvailableCommands_AvailableCommands_IsValid;
        if (!SetAvailableCommands_IsValid)
            Logging.LogError("Function WBP_CommandConsole_C:SetAvailableCommands is not valid.");

        AddMessage_FunctionAddress = NativeReflectionCached.GetFunction(@class, "AddMessage");
        AddMessage_ParamsSize = NativeReflection.GetFunctionParamsSize(AddMessage_FunctionAddress);
        NativeReflectionCached.GetPropertyRef(ref AddMessage_Message_PropertyAddress, AddMessage_FunctionAddress, "Message");
        AddMessage_Message_Offset = NativeReflectionCached.GetPropertyOffset(AddMessage_FunctionAddress, "Message");
        AddMessage_Message_IsValid = NativeReflectionCached.ValidatePropertyClass(AddMessage_FunctionAddress, "Message", Classes.FStrProperty);
        AddMessage_IsValid = AddMessage_FunctionAddress != IntPtr.Zero && AddMessage_Message_IsValid;
        if (!AddMessage_IsValid)
            Logging.LogError("Function WBP_CommandConsole_C:AddMessage is not valid.");
    }
}
