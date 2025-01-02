using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using b1;
using CsB1;
using HarmonyLib;
using UnrealEngine.Engine;
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

        private static void Prefix(float DeltaTime, int TickGroup)
        {
            try
            {
                // MyMod.Instance.Photon.DispatchIncomingEvents();
            }
            catch (Exception ex)
            {
                WukongClient.Log("PatchTickWithGroup Prefix Error {ex}");
            }
        }

        private static void Postfix(float DeltaTime, int TickGroup)
        {
            try
            {
                Global.TickWithGroup(DeltaTime);
                // MyMod.Instance.Photon.SendOutgoingCommands();
            }
            catch (Exception ex)
            {
                WukongClient.Log("PatchTickWithGroup Postfix Error {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPCharacterData), nameof(BUC_ABPCharacterData.Update_GameThread))]
    public class PatchPlayerAnimation
    {
        private static void Postfix(BUC_ABPCharacterData __instance, AActor Owner, IBUC_ABPHelperData HelperData, float DeltaTime)
        {
            // // WukongClient.Log($"Called Update_GameThread for {Owner.GetName()} ({Owner.GetEntityHash()})");
            //
            // var photon = MyMod.Instance.Photon;
            //
            // if (photon == null)
            // {
            //     return;
            // }
            //
            // if (Owner == photon.LocalPlayerState.Pawn)
            // {
            //     var localState = photon.LocalPlayerState;
            //
            //     if (localState.LastIsFalling != __instance.IsFalling)
            //     {
            //         photon.LocalPlayerState.LastIsFalling = __instance.IsFalling;
            //         photon.SendIsFalling(photon.LocalPlayerState.LastIsFalling);
            //         WukongClient.Log($"Sent IsFalling ({photon.LocalPlayerState.LastIsFalling})");
            //     }
            // }
            //
            // var pawn = photon.GetByActor(Owner);
            //
            // if (pawn == null)
            // {
            //     // WukongClient.Log($"Could not find player for {Owner.GetName()} ({Owner.GetEntityHash()})");
            //     // foreach (var (key, value) in photon.ConnectedPlayers)
            //     // {
            //     //     WukongClient.Log($"{key}: {value.Pawn.GetName()} ({value.Pawn.GetEntityHash()})");
            //     // }
            //     //
            //     return;
            // }
            //
            // __instance.IsFalling = pawn.LastIsFalling;
            // WukongClient.Log($"Set IsFalling to {pawn.LastIsFalling} for ({Owner.GetEntityHash()})");
            
            __instance.IsFalling = true;
        }
    }
}