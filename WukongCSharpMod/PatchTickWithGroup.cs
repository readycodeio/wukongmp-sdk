using System;
using System.Reflection;
using b1;
using HarmonyLib;
using UnrealEngine.Engine;

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
                    photon.SendMoveAcceleration(photon.LocalPlayerState.ActorLocation);
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
            MyMod.Instance.Photon?.SendUpdatedPlayerProperties();
        }
    }
}