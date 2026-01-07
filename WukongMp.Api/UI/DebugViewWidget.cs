using System;
using UnrealEngine.Runtime;

namespace WukongMp.Api.UI
{
    public class DebugViewWidget : GameWidgetBase
    {
        private const string DebugViewWidgetPath = "/Game/Mods/WukongMod/Debug/WBP_DebugView.WBP_DebugView_C";

        public DebugViewWidget() : base(DebugViewWidgetPath) { }

        public void SetVersionText(string version)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetVersionText {version}", true);
        }

        public void AddPlayer(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"AddPlayer {playerName}", true);
        }

        public void RemovePlayer(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"RemovePlayer {playerName}", true);
        }

        public void ClearPlayers(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"ClearPlayers {playerName}", true);
        }

        public void ToggleVisibility()
        {
            GameWidget?.CallFunctionByNameWithArguments($"ToggleVisibility", true);
        }

        public unsafe void SetPlayerPosition(string playerName, FVector gameLocation, FVector ecsLocation)
        {
            if (GameWidget == null || SetPlayerPosition_PlayerName_PropertyAddress == null || SetPlayerPosition_GameLocation_PropertyAddress == null || SetPlayerPosition_EcsLocation_PropertyAddress == null)
            {
                Logging.LogError("GameWidget or property address is null in WBP_DebugView:SetPlayerPosition.");
                return;
            }

            if (!SetPlayerPosition_IsValid)
            {
                Logging.LogError("Function WBP_DebugView:SetPlayerPosition is not valid.");
                return;
            }

            byte* ptr = stackalloc byte[(int)(uint)(SetPlayerPosition_ParamsSize + 16)];
            int num = (int)((16L - (long)ptr) & 0xF);
            byte* ptr2 = ptr + num;
            System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)SetPlayerPosition_ParamsSize);
            IntPtr intPtr = new IntPtr(ptr2);

            FStringMarshaler.ToNative(IntPtr.Add(intPtr, SetPlayerPosition_PlayerName_Offset), 0, SetPlayerPosition_PlayerName_PropertyAddress.Address, playerName);
            BlittableTypeMarshaler<FVector>.ToNative(IntPtr.Add(intPtr, SetPlayerPosition_GameLocation_Offset), 0, SetPlayerPosition_GameLocation_PropertyAddress.Address, gameLocation);
            BlittableTypeMarshaler<FVector>.ToNative(IntPtr.Add(intPtr, SetPlayerPosition_EcsLocation_Offset), 0, SetPlayerPosition_EcsLocation_PropertyAddress.Address, ecsLocation);

            NativeReflection.InvokeStaticFunctionOptimized(GameWidget.Address, SetPlayerPosition_FunctionAddress, intPtr, SetPlayerPosition_ParamsSize);
            NativeReflection.DestroyValue_InContainer(SetPlayerPosition_PlayerName_PropertyAddress.Address, intPtr);
        }

        public unsafe bool IsVisible()
        {
            if (GameWidget == null || IsWidgetVisible_ReturnValue_PropertyAddress == null)
            {
                Logging.LogDebug("GameWidget or property address is null in WBP_DebugView_C:IsWidgetVisible.");
                return false;
            }

            if (!IsWidgetVisible_IsValid)
            {
                Logging.LogError("Function WBP_DebugView_C:IsWidgetVisible is not valid.");
                return false;
            }

            byte* ptr = stackalloc byte[(int)(uint)(IsWidgetVisible_ParamsSize + 16)];
            int num = (int)((16L - (long)ptr) & 0xF);
            byte* ptr2 = ptr + num;
            System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)IsWidgetVisible_ParamsSize);
            IntPtr intPtr = new IntPtr(ptr2);

            NativeReflection.InvokeFunctionOptimized(GameWidget.Address, IsWidgetVisible_FunctionAddress, intPtr, IsWidgetVisible_ParamsSize);
            return BlittableTypeMarshaler<bool>.FromNative(IntPtr.Add(intPtr, IsWidgetVisible_ReturnValue_Offset), 0, IsWidgetVisible_ReturnValue_PropertyAddress.Address);
        }

        protected override void PostInitialize()
        {
            InitNativeFunctions();
        }

        static DebugViewWidget()
        {
            InitNativeFunctions();
        }

        private static bool SetPlayerPosition_IsValid;
        private static IntPtr SetPlayerPosition_FunctionAddress;
        private static int SetPlayerPosition_ParamsSize;

        private static int SetPlayerPosition_PlayerName_Offset;
        private static bool SetPlayerPosition_PlayerName_IsValid;
        private static FFieldAddress? SetPlayerPosition_PlayerName_PropertyAddress;

        private static int SetPlayerPosition_GameLocation_Offset;
        private static bool SetPlayerPosition_GameLocation_IsValid;
        private static FFieldAddress? SetPlayerPosition_GameLocation_PropertyAddress;

        private static int SetPlayerPosition_EcsLocation_Offset;
        private static bool SetPlayerPosition_EcsLocation_IsValid;
        private static FFieldAddress? SetPlayerPosition_EcsLocation_PropertyAddress;


        private static bool IsWidgetVisible_IsValid;
        private static IntPtr IsWidgetVisible_FunctionAddress;
        private static int IsWidgetVisible_ParamsSize;

        private static bool IsWidgetVisible_ReturnValue_IsValid;
        private static FFieldAddress? IsWidgetVisible_ReturnValue_PropertyAddress;
        private static int IsWidgetVisible_ReturnValue_Offset;


        public static void InitNativeFunctions()
        {
            IntPtr classPtr = NativeReflection.GetClass(DebugViewWidgetPath);
            SetPlayerPosition_FunctionAddress = NativeReflectionCached.GetFunction(classPtr, "SetPlayerPosition");
            SetPlayerPosition_ParamsSize = NativeReflection.GetFunctionParamsSize(SetPlayerPosition_FunctionAddress);

            NativeReflectionCached.GetPropertyRef(ref SetPlayerPosition_PlayerName_PropertyAddress, SetPlayerPosition_FunctionAddress, "PlayerName");
            SetPlayerPosition_PlayerName_Offset = NativeReflectionCached.GetPropertyOffset(SetPlayerPosition_FunctionAddress, "PlayerName");
            SetPlayerPosition_PlayerName_IsValid = NativeReflectionCached.ValidatePropertyClass(SetPlayerPosition_FunctionAddress, "PlayerName", Classes.FStrProperty);

            NativeReflectionCached.GetPropertyRef(ref SetPlayerPosition_GameLocation_PropertyAddress, SetPlayerPosition_FunctionAddress, "GameLocation");
            SetPlayerPosition_GameLocation_Offset = NativeReflectionCached.GetPropertyOffset(SetPlayerPosition_FunctionAddress, "GameLocation");
            SetPlayerPosition_GameLocation_IsValid = NativeReflectionCached.ValidatePropertyClass(SetPlayerPosition_FunctionAddress, "GameLocation", Classes.FStructProperty);

            NativeReflectionCached.GetPropertyRef(ref SetPlayerPosition_EcsLocation_PropertyAddress, SetPlayerPosition_FunctionAddress, "EcsLocation");
            SetPlayerPosition_EcsLocation_Offset = NativeReflectionCached.GetPropertyOffset(SetPlayerPosition_FunctionAddress, "EcsLocation");
            SetPlayerPosition_EcsLocation_IsValid = NativeReflectionCached.ValidatePropertyClass(SetPlayerPosition_FunctionAddress, "EcsLocation", Classes.FStructProperty);

            SetPlayerPosition_IsValid = SetPlayerPosition_FunctionAddress != IntPtr.Zero && SetPlayerPosition_PlayerName_IsValid && SetPlayerPosition_GameLocation_IsValid && SetPlayerPosition_EcsLocation_IsValid;
            if (!SetPlayerPosition_IsValid)
                Logging.LogError("Function WBP_DebugView_C:SetPlayerPosition is not valid.");


            IsWidgetVisible_FunctionAddress = NativeReflectionCached.GetFunction(classPtr, "IsWidgetVisible");
            IsWidgetVisible_ParamsSize = NativeReflection.GetFunctionParamsSize(IsWidgetVisible_FunctionAddress);

            NativeReflectionCached.GetPropertyRef(ref IsWidgetVisible_ReturnValue_PropertyAddress, IsWidgetVisible_FunctionAddress, "IsVisible");
            IsWidgetVisible_ReturnValue_Offset = NativeReflectionCached.GetPropertyOffset(IsWidgetVisible_FunctionAddress, "IsVisible");
            IsWidgetVisible_ReturnValue_IsValid = NativeReflectionCached.ValidatePropertyClass(IsWidgetVisible_FunctionAddress, "IsVisible", Classes.FBoolProperty);
            IsWidgetVisible_IsValid = IsWidgetVisible_FunctionAddress != IntPtr.Zero && IsWidgetVisible_ReturnValue_IsValid;
            if (!IsWidgetVisible_IsValid)
                Logging.LogError("Function WBP_DebugView_C:IsWidgetVisible is not valid.");
        }
    }
}
