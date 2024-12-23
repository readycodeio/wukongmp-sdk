using System;
using System.Collections.Generic;
using System.Reflection;
using CsB1;
using HarmonyLib;
using WukongMp.Common;

namespace WukongCSharpMod
{
    [HarmonyPatch]
    public class PatchTickWithGroup
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BGS_TamerManagerSystem:OnTickWithGroup");
        }

        private static void Postfix(float DeltaTime, int TickGroup)
        {
            try
            {
                Global.TickWithGroup(DeltaTime);
            }
            catch (Exception ex)
            {
                WukongClient.Log("PatchTickWithGroup Postfix Error {ex}");
            }
        }
    }
    
    [HarmonyPatch]
    public class PatchRpc
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BGP_PlayerControllerCS:GSRpcSendClient");
        }

        private static void Postfix(List<byte> SendData)
        {
            WukongClient.Log($"GSRpcSendClient: {SendData.Count} bytes");
        }
    }
    
    [HarmonyPatch]
    public class PatchServerRpc
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BGP_PlayerControllerCS:GSRpcSendServer");
        }

        private static void Postfix(List<byte> SendData)
        {
            WukongClient.Log($"GSRpcSendServer: {SendData.Count} bytes");
        }
    }
    
    [HarmonyPatch]
    public class PatchRpcCallback
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("CsB1.CSRpc:InvokeRpcCallBack");
        }

        private static void Postfix(CSRpcAsyncRequest CSRpcRequestTask, bool Timeout = false)
        {
            var cmd = CSRpcRequestTask.CSMsgReq.Head.Cmd;
            WukongClient.Log($"RPC callback: {cmd} Timeout: {Timeout}");
        }
    }
}