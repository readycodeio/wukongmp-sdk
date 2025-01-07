using System;
using System.Reflection;
using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Plugins.EnhancedInput;
using UnrealEngine.Runtime;
using FInputActionValue = b1.FInputActionValue;

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

    [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnTriggerInputActionImpl")]
    public class PatchPlayerInputs
    {
        public static void Postfix(
            string ActionName,
            ETriggerEvent TriggerEvent,
            FInputActionValue Value)
        {
            var photon = MyMod.Instance.Photon;

            if (photon == null)
            {
                return;
            }

            KeyState keyState;
            PlayerInput key;

            Helpers.Log($"Action: {ActionName}, TriggerEvent: {TriggerEvent}, Value: {Value}");

            switch (ActionName)
            {
                case "IA_B1LightAttack":
                    key = PlayerInput.LightAttack;
                    keyState = TriggerEvent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1HeavyAttack":
                    key = PlayerInput.HeavyAttack;
                    keyState = TriggerEvent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                default:
                    return;
            }

            photon.SendKeyPressed(key, keyState);
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
                    photon.SendIsFlying(photon.LocalPlayerState.IsFlying);
                    Helpers.Log($"Sent IsFlying ({photon.LocalPlayerState.IsFlying})");
                }

                if (localState.IsFalling != __instance.IsFalling)
                {
                    photon.LocalPlayerState.IsFalling = __instance.IsFalling;
                    photon.SendIsFalling(photon.LocalPlayerState.IsFalling);
                    Helpers.Log($"Sent IsFalling ({photon.LocalPlayerState.IsFalling})");
                }

                if (localState.IsLandingMove != __instance.IsLandingMove)
                {
                    photon.LocalPlayerState.IsLandingMove = __instance.IsLandingMove;
                    photon.SendIsLandingMove(photon.LocalPlayerState.IsLandingMove);
                    Helpers.Log($"Sent IsLandingMove ({photon.LocalPlayerState.IsLandingMove})");
                }

                if (!localState.Velocity.Equals(__instance.Velocity, Tolerance))
                {
                    // fix running in place
                    if (__instance.Velocity.Size() < 1f)
                    {
                        __instance.Velocity = FVector.ZeroVector;
                    }

                    photon.LocalPlayerState.Velocity = __instance.Velocity;
                    photon.SendVelocity(photon.LocalPlayerState.Velocity);
                    Helpers.Log($"Sent Velocity ({photon.LocalPlayerState.Velocity})");
                }

                if (!localState.MoveAcceleration.Equals(__instance.MoveAcceleration, Tolerance))
                {
                    photon.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    photon.SendMoveAcceleration(photon.LocalPlayerState.MoveAcceleration);
                    Helpers.Log($"Sent MoveAcceleration ({photon.LocalPlayerState.MoveAcceleration})");
                }

                if (!localState.ActorLocation.Equals(__instance.ActorLocation, Tolerance))
                {
                    photon.LocalPlayerState.ActorLocation = __instance.ActorLocation;
                    photon.SendActorLocation(photon.LocalPlayerState.ActorLocation);
                    Helpers.Log($"Sent ActorLocation ({photon.LocalPlayerState.ActorLocation})");
                }
            }
            else
            {
                var playerState = photon.GetByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.IsFlying = playerState.IsFlying;
                __instance.IsFalling = playerState.IsFalling;
                __instance.IsLandingMove = playerState.IsLandingMove;
                __instance.Velocity = playerState.Velocity;
                __instance.MoveAcceleration = playerState.MoveAcceleration;

                var events = BUS_EventCollectionCS.Get(Owner);
                events.Evt_InterpolationMove.Invoke(playerState.ActorLocation, FRotator.ZeroRotator, 0.033f, true, false, true, true);
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

                if (localState.TurnInplaceTargetRotation != __instance.TurnInplaceTargetRotation)
                {
                    photon.LocalPlayerState.TurnInplaceTargetRotation = __instance.TurnInplaceTargetRotation;
                    photon.SendTurnInplaceTargetRotation(photon.LocalPlayerState.TurnInplaceTargetRotation);
                    Helpers.Log($"Sent TurnInplaceTargetRotation ({photon.LocalPlayerState.TurnInplaceTargetRotation})");
                }
            }
            else
            {
                var playerState = photon.GetByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.TurnInplaceTargetRotation = playerState.TurnInplaceTargetRotation;
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
                    photon.SendInJump(photon.LocalPlayerState.InJump);
                    Helpers.Log($"Sent InJump ({photon.LocalPlayerState.InJump})");
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