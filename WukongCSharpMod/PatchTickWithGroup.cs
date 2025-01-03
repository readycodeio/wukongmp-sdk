using System;
using System.Reflection;
using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

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
                Helpers.Log("PatchTickWithGroup Prefix Error {ex}");
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
                Helpers.Log("PatchTickWithGroup Postfix Error {ex}");
            }
        }
    }

    // [HarmonyPatch(typeof(BUC_ABPPlayerLocomotionData), nameof(BUC_ABPPlayerLocomotionData.Update))]
    // public class LocomotionPatch
    // {
    //     private const float Tolerance = 0.01f;
    //     
    //     private static void Prefix(
    //         BUC_ABPPlayerLocomotionData __instance,
    //         AActor Owner,
    //         IBUC_ABPCommonSettingData CommonData,
    //         IBUC_ABPBasicData BasicData,
    //         IBUC_ABPCharacterData ChrData,
    //         IBUC_ABPBGUCharacterData BGUData,
    //         IBUC_ABPCommonLocomotionData LocomotionData,
    //         IBUC_ABPSpecialMoveData SpecialMoveData,
    //         IBUC_ABPHelperData HelperData,
    //         float DeltaTime)
    //     {
    //         var characterData = (BUC_ABPCharacterData)ChrData;
    //     }
    // }

    [HarmonyPatch(typeof(BUC_ABPCharacterData), nameof(BUC_ABPCharacterData.Update_GameThread))]
    public class PatchPlayerAnimation
    {
        private const float Tolerance = 0.01f;

        public static void Postfix(BUC_ABPCharacterData __instance, AActor Owner, IBUC_ABPHelperData HelperData, float DeltaTime)
        {
            var photon = MyMod.Instance.Photon;

            if (photon == null)
            {
                return;
            }

            if (Owner == photon.LocalPlayerState.Pawn)
            {
                var localState = photon.LocalPlayerState;

                if (localState.IsFlying != __instance.IsFlying)
                {
                    photon.LocalPlayerState.IsFlying = __instance.IsFlying;
                    photon.SendIsFlying(photon.LocalPlayerState.IsFlying);
                    Helpers.Log($"Sent IsFlying ({photon.LocalPlayerState.IsFlying})");
                }

                if (localState.IsFalling != __instance.IsFalling)
                {
                    photon.LocalPlayerState.IsFalling = __instance.IsFalling;
                    photon.SendIsFalling(photon.LocalPlayerState.IsFalling);
                    Helpers.Log($"Sent IsFalling ({photon.LocalPlayerState.IsFalling})");
                }

                if (localState.IsLastFrameFalling != __instance.IsLastFrameFalling)
                {
                    photon.LocalPlayerState.IsLastFrameFalling = __instance.IsLastFrameFalling;
                    photon.SendIsLastFrameFalling(photon.LocalPlayerState.IsLastFrameFalling);
                    Helpers.Log($"Sent IsLastFrameFalling ({photon.LocalPlayerState.IsLastFrameFalling})");
                }

                if (localState.IsLandingMove != __instance.IsLandingMove)
                {
                    photon.LocalPlayerState.IsLandingMove = __instance.IsLandingMove;
                    photon.SendIsLandingMove(photon.LocalPlayerState.IsLandingMove);
                    Helpers.Log($"Sent IsLandingMove ({photon.LocalPlayerState.IsLandingMove})");
                }

                // if (!localState.ActorLocation.Equals(__instance.ActorLocation, Tolerance))
                // {
                //     photon.LocalPlayerState.ActorLocation = __instance.ActorLocation;
                //     photon.SendActorLocation(photon.LocalPlayerState.ActorLocation);
                //     Helpers.Log($"Sent ActorLocation ({photon.LocalPlayerState.ActorLocation})");
                // }
                //
                // if (!localState.ActorRotation.Equals(__instance.ActorRotation, Tolerance))
                // {
                //     photon.LocalPlayerState.ActorRotation = __instance.ActorRotation;
                //     photon.SendActorRotation(photon.LocalPlayerState.ActorRotation);
                //     Helpers.Log($"Sent ActorRotation ({photon.LocalPlayerState.ActorRotation})");
                // }
                //
                // if (!localState.ForwardVector.Equals(__instance.ForwardVector, Tolerance))
                // {
                //     photon.LocalPlayerState.ForwardVector = __instance.ForwardVector;
                //     photon.SendForwardVector(photon.LocalPlayerState.ForwardVector);
                //     Helpers.Log($"Sent ForwardVector ({photon.LocalPlayerState.ForwardVector})");
                // }
                //
                if (!localState.Velocity.Equals(__instance.Velocity, Tolerance))
                {
                    photon.LocalPlayerState.Velocity = __instance.Velocity;
                    photon.SendVelocity(photon.LocalPlayerState.Velocity);
                    Helpers.Log($"Sent Velocity ({photon.LocalPlayerState.Velocity})");
                }
                //
                // if (!localState.LeftFootPos.Equals(__instance.LeftFootPos, Tolerance))
                // {
                //     photon.LocalPlayerState.LeftFootPos = __instance.LeftFootPos;
                //     photon.SendLeftFootPos(photon.LocalPlayerState.LeftFootPos);
                //     Helpers.Log($"Sent LeftFootPos ({photon.LocalPlayerState.LeftFootPos})");
                // }
                //
                // if (!localState.RightFootPos.Equals(__instance.RightFootPos, Tolerance))
                // {
                //     photon.LocalPlayerState.RightFootPos = __instance.RightFootPos;
                //     photon.SendRightFootPos(photon.LocalPlayerState.RightFootPos);
                //     Helpers.Log($"Sent RightFootPos ({photon.LocalPlayerState.RightFootPos})");
                // }
                
                if (!localState.MoveAcceleration.Equals(__instance.MoveAcceleration, Tolerance))
                {
                    photon.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    photon.SendMoveAcceleration(photon.LocalPlayerState.MoveAcceleration);
                    Helpers.Log($"Sent MoveAcceleration ({photon.LocalPlayerState.MoveAcceleration})");
                }
            }

            var playerState = photon.GetByActor(Owner);

            if (playerState == null)
            {
                return;
            }

            __instance.IsFlying = playerState.IsFlying;
            __instance.IsFalling = playerState.IsFalling;
            __instance.IsLastFrameFalling = playerState.IsLastFrameFalling;
            __instance.IsLandingMove = playerState.IsLandingMove;
            // __instance.ActorLocation = playerState.ActorLocation;
            // __instance.ActorRotation = playerState.ActorRotation;
            // __instance.ForwardVector = playerState.ForwardVector;
            __instance.Velocity = playerState.Velocity;
            // __instance.LeftFootPos = playerState.LeftFootPos;
            // __instance.RightFootPos = playerState.RightFootPos;
            __instance.MoveAcceleration = playerState.MoveAcceleration;
        }
    }
    
    [HarmonyPatch(typeof(BUC_ABPBasicData), nameof(BUC_ABPBasicData.Update_WorkThread))]
    public class PatchBasicData
    {
        private const float Tolerance = 0.01f;

        public static void Postfix(
            BUC_ABPBasicData __instance,
            AActor Owner,
            IBUC_ABPCharacterData ChrData,
            IBUC_ABPBGUCharacterData BGUData,
            IBUC_SpeedCtrlData SpeedCtrlData,
            float DeltaTime)
        {
            var photon = MyMod.Instance.Photon;

            if (photon == null)
            {
                return;
            }

            if (Owner == photon.LocalPlayerState.Pawn)
            {
                var localState = photon.LocalPlayerState;

                // if (Math.Abs(localState.VerticalSpeed - __instance.VerticleSpeed) > Tolerance)
                // {
                //     photon.LocalPlayerState.VerticalSpeed = __instance.VerticleSpeed;
                //     photon.SendVerticalSpeed(photon.LocalPlayerState.VerticalSpeed);
                //     Helpers.Log($"Sent VerticalSpeed ({photon.LocalPlayerState.VerticalSpeed})");
                // }
            }

            var playerState = photon.GetByActor(Owner);

            if (playerState == null)
            {
                return;
            }

            // __instance.VerticleSpeed = playerState.VerticalSpeed;
        }
    }
}