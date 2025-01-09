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

        private static void Postfix(float DeltaTime, int TickGroup)
        {
            try
            {
                Global.TickWithGroup(DeltaTime);
            }
            catch (Exception ex)
            {
                Helpers.Log("PatchTickWithGroup Postfix Error {ex}");
            }
        }
    }

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
                    photon.SetPlayerProperty(nameof(PlayerState.IsFlying), photon.LocalPlayerState.IsFlying);
                }

                if (localState.IsFalling != __instance.IsFalling)
                {
                    photon.LocalPlayerState.IsFalling = __instance.IsFalling;
                    photon.SetPlayerProperty(nameof(PlayerState.IsFalling), photon.LocalPlayerState.IsFalling);
                }

                if (localState.IsLandingMove != __instance.IsLandingMove)
                {
                    photon.LocalPlayerState.IsLandingMove = __instance.IsLandingMove;
                    photon.SetPlayerProperty(nameof(PlayerState.IsLandingMove), photon.LocalPlayerState.IsLandingMove);
                }

                if (!localState.Velocity.Equals(__instance.Velocity, Tolerance))
                {
                    photon.LocalPlayerState.Velocity = __instance.Velocity;
                    photon.SetPlayerProperty(nameof(PlayerState.Velocity), photon.LocalPlayerState.Velocity);
                }

                if (!localState.MoveAcceleration.Equals(__instance.MoveAcceleration, Tolerance))
                {
                    photon.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    photon.SetPlayerProperty(nameof(PlayerState.MoveAcceleration), photon.LocalPlayerState.MoveAcceleration);
                }

                if (!localState.ActorLocation.Equals(__instance.ActorLocation, Tolerance))
                {
                    photon.LocalPlayerState.ActorLocation = __instance.ActorLocation;
                    photon.SetPlayerProperty(nameof(PlayerState.ActorLocation), photon.LocalPlayerState.ActorLocation);
                }

                if (!localState.ActorRotation.Equals(__instance.ActorRotation, Tolerance))
                {
                    photon.LocalPlayerState.ActorRotation = __instance.ActorRotation;
                    photon.SetPlayerProperty(nameof(PlayerState.ActorRotation), photon.LocalPlayerState.ActorRotation);
                }
            }
            else
            {
                var playerState = photon.GetByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                var events = BUS_EventCollectionCS.Get(Owner);

                __instance.IsFlying = playerState.IsFlying;
                __instance.IsFalling = playerState.IsFalling;
                __instance.IsLandingMove = playerState.IsLandingMove;

                __instance.Velocity = playerState.Velocity;
                if (__instance.Velocity.IsNearlyZero())
                {
                    __instance.Velocity = FVector.ZeroVector;
                    playerState.Velocity = FVector.ZeroVector;
                    __instance.MovementComp.Velocity = FVector.ZeroVector;
                    events.Evt_StopCurrentMove.Invoke();
                }

                __instance.MoveAcceleration = playerState.MoveAcceleration;
                if (__instance.MoveAcceleration.IsNearlyZero())
                {
                    __instance.MoveAcceleration = FVector.ZeroVector;
                    playerState.MoveAcceleration = FVector.ZeroVector;
                }

                events.Evt_InterpolationMove.Invoke(playerState.ActorLocation, playerState.ActorRotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPBGUCharacterData), nameof(BUC_ABPBGUCharacterData.Update_GameThread))]
    public class PatchBGUPlayerAnimation
    {
        private const float Tolerance = 0.01f;

        public static void Postfix(
            BUC_ABPBGUCharacterData __instance,
            AActor Owner,
            IBUC_ABPCharacterData ChrData,
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

                if (localState.IsStandRotate != __instance.IsStandRotate)
                {
                    photon.LocalPlayerState.IsStandRotate = __instance.IsStandRotate;
                    photon.SetPlayerProperty(nameof(PlayerState.IsStandRotate), photon.LocalPlayerState.IsStandRotate);
                }

                if (localState.IsAttacking != __instance.IsAttacking)
                {
                    photon.LocalPlayerState.IsAttacking = __instance.IsAttacking;
                    photon.SetPlayerProperty(nameof(PlayerState.IsAttacking), photon.LocalPlayerState.IsAttacking);
                }

                if (!localState.TurnInplaceTargetRotation.Equals(__instance.TurnInplaceTargetRotation, Tolerance))
                {
                    photon.LocalPlayerState.TurnInplaceTargetRotation = __instance.TurnInplaceTargetRotation;
                    photon.SetPlayerProperty(nameof(PlayerState.TurnInplaceTargetRotation), photon.LocalPlayerState.TurnInplaceTargetRotation);
                }

                if (MathF.Abs(localState.TurnInplaceRemainAngle - __instance.TurnInplaceRemainAngle) > Tolerance)
                {
                    photon.LocalPlayerState.TurnInplaceRemainAngle = __instance.TurnInplaceRemainAngle;
                    photon.SetPlayerProperty(nameof(PlayerState.TurnInplaceRemainAngle), photon.LocalPlayerState.TurnInplaceRemainAngle);
                }

                if (localState.OrientRotationToMovement != __instance.bOrientRotationToMovement)
                {
                    photon.LocalPlayerState.OrientRotationToMovement = __instance.bOrientRotationToMovement;
                    photon.SetPlayerProperty(nameof(PlayerState.OrientRotationToMovement), photon.LocalPlayerState.OrientRotationToMovement);
                }
            }
            else
            {
                var playerState = photon.GetByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.IsStandRotate = playerState.IsStandRotate;
                __instance.IsAttacking = playerState.IsAttacking;
                __instance.TurnInplaceTargetRotation = playerState.TurnInplaceTargetRotation;
                __instance.TurnInplaceRemainAngle = playerState.TurnInplaceRemainAngle;
                __instance.bOrientRotationToMovement = playerState.OrientRotationToMovement;
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPJumpV2Data), nameof(BUC_ABPJumpV2Data.Update))]
    public class PatchJumpData
    {
        private const float Tolerance = 0.01f;

        public static void Postfix(
            BUC_ABPJumpV2Data __instance,
            AActor Owner,
            IBUC_ActorBasicData ActorBasicData,
            IBUC_ABPCharacterData ChrData,
            IBUC_ABPBasicData BasicData,
            IBUC_ABPSpecialMoveData SpecialMoveData,
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

                if (localState.InJump != __instance.bInJump)
                {
                    photon.LocalPlayerState.InJump = __instance.bInJump;
                    photon.SetPlayerProperty(nameof(PlayerState.InJump), photon.LocalPlayerState.InJump);
                }
            }
            else
            {
                var playerState = photon.GetByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.bInJump = playerState.InJump;
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPBasicData), nameof(BUC_ABPBasicData.Update_WorkThread))]
    public class PatchBasicData
    {
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

                if (localState.MoveSpeedLevel != __instance.MoveSpeedLevel)
                {
                    photon.LocalPlayerState.MoveSpeedLevel = __instance.MoveSpeedLevel;
                    photon.SetPlayerProperty(nameof(PlayerState.MoveSpeedLevel), photon.LocalPlayerState.MoveSpeedLevel);
                }

                if (localState.MoveSpeedState != __instance.MoveSpeedState)
                {
                    photon.LocalPlayerState.MoveSpeedState = __instance.MoveSpeedState;
                    photon.SetPlayerProperty(nameof(PlayerState.MoveSpeedState), photon.LocalPlayerState.MoveSpeedState);
                }
            }
            else
            {
                var playerState = photon.GetByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.MoveSpeedLevel = playerState.MoveSpeedLevel;
                __instance.MoveSpeedState = playerState.MoveSpeedState;
            }
        }
    }

    [HarmonyPatch(typeof(BUS_ABPHelperComp), "OnTickImpl")]
    public class PatchTick
    {
        public static void Postfix(float DeltaTime, bool IsThreadTick)
        {
            if (IsThreadTick)
            {
                MyMod.Instance.Photon?.SendUpdatedPlayerProperties();
            }
        }
    }
}