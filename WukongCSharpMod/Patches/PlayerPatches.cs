using System;
using b1;
using BtlShare;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(BUC_ABPCharacterData), nameof(BUC_ABPCharacterData.Update_GameThread))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchPlayerAnimation
    {
        public static void Postfix(BUC_ABPCharacterData __instance, AActor Owner, IBUC_ABPHelperData HelperData, float DeltaTime)
        {
            if (!(Owner is BGUCharacterCS character))
                return;

            var photon = MyMod.Instance.Photon;

            if (character == photon.LocalPlayerState.Pawn)
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

                if (!localState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.Velocity = __instance.Velocity;
                    photon.SetPlayerProperty(nameof(PlayerState.Velocity), photon.LocalPlayerState.Velocity);
                }

                if (!localState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    photon.SetPlayerProperty(nameof(PlayerState.MoveAcceleration), photon.LocalPlayerState.MoveAcceleration);
                }

                if (!localState.ActorLocation.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.ActorLocation = __instance.ActorLocation;
                    photon.SetPlayerProperty(nameof(PlayerState.ActorLocation), photon.LocalPlayerState.ActorLocation);
                }

                if (!localState.ActorRotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.ActorRotation = __instance.ActorRotation;
                    photon.SetPlayerProperty(nameof(PlayerState.ActorRotation), photon.LocalPlayerState.ActorRotation);
                }
            }
            else
            {
                var playerState = photon.GetByActor(character);

                if (playerState != null)
                {
                    var events = BUS_EventCollectionCS.Get(character);

                    __instance.IsFlying = playerState.IsFlying;
                    __instance.IsFalling = playerState.IsFalling;
                    __instance.IsLandingMove = playerState.IsLandingMove;
                    __instance.Velocity = playerState.Velocity;

                    if (__instance.Velocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                    {
                        __instance.Velocity = FVector.ZeroVector;
                        playerState.Velocity = FVector.ZeroVector;
                    }

                    __instance.MoveAcceleration = playerState.MoveAcceleration;
                    if (__instance.MoveAcceleration.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                    {
                        __instance.MoveAcceleration = FVector.ZeroVector;
                        playerState.MoveAcceleration = FVector.ZeroVector;
                    }

                    if (!playerState.ActorLocation.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                    {
                        events.Evt_InterpolationMove.Invoke(playerState.ActorLocation, playerState.ActorRotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                    }
                }
                else
                {
                    // maybe it's a monster
                    var monsterState = photon.GetMonsterByCharacter(character);

                    if (monsterState != null)
                    {
                        if (photon.IsMasterClient)
                        {
                            Helpers.Log("Will send monster movement data");
                            if (!monsterState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                            {
                                monsterState.Velocity = __instance.Velocity;
                                Helpers.Log("Will send velocity");
                                photon.SetMonsterProperty(monsterState.Id, nameof(MonsterState.Velocity), monsterState.Velocity);
                            }

                            if (!monsterState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                            {
                                monsterState.MoveAcceleration = __instance.MoveAcceleration;
                                Helpers.Log("Will send move acceleration");
                                photon.SetMonsterProperty(monsterState.Id, nameof(MonsterState.MoveAcceleration), monsterState.MoveAcceleration);
                            }
                        }
                        else
                        {
                            Helpers.Log("Received monster movement data");
                            __instance.Velocity = monsterState.Velocity;
                            __instance.MoveAcceleration = monsterState.MoveAcceleration;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPBGUCharacterData), nameof(BUC_ABPBGUCharacterData.Update_GameThread))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchBGUPlayerAnimation
    {
        public static void Postfix(
            BUC_ABPBGUCharacterData __instance,
            AActor Owner,
            IBUC_ABPCharacterData ChrData,
            IBUC_SpeedCtrlData SpeedCtrlData,
            float DeltaTime)
        {
            if (!(Owner is BGUCharacterCS))
                return;

            var photon = MyMod.Instance.Photon;

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

                if (!localState.TurnInplaceTargetRotation.Equals(__instance.TurnInplaceTargetRotation, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.TurnInplaceTargetRotation = __instance.TurnInplaceTargetRotation;
                    photon.SetPlayerProperty(nameof(PlayerState.TurnInplaceTargetRotation), photon.LocalPlayerState.TurnInplaceTargetRotation);
                }

                if (localState.TurnInplaceRemainAngle.Equals(__instance.TurnInplaceRemainAngle, Constants.FloatComparisonTolerance))
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

    [HarmonyPatch(typeof(BUC_ABPPlayerLocomotionData), nameof(BUC_ABPPlayerLocomotionData.Update))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchPlayerLocomotion
    {
        public static void Postfix(
            BUC_ABPPlayerLocomotionData __instance,
            AActor Owner,
            IBUC_ABPCommonSettingData CommonData,
            IBUC_ABPBasicData BasicData,
            IBUC_ABPCharacterData ChrData,
            IBUC_ABPBGUCharacterData BGUData,
            IBUC_ABPCommonLocomotionData LocomotionData,
            IBUC_ABPSpecialMoveData SpecialMoveData,
            IBUC_ABPHelperData HelperData,
            float DeltaTime)
        {
            if (!(Owner is BGUCharacterCS))
                return;

            var photon = MyMod.Instance.Photon;

            if (Owner == photon.LocalPlayerState.Pawn)
            {
                var localState = photon.LocalPlayerState;

                if (localState.ShouldWaitRotateFinished != __instance.bShouldWaitRotateFinished)
                {
                    photon.LocalPlayerState.ShouldWaitRotateFinished = __instance.bShouldWaitRotateFinished;
                    photon.SetPlayerProperty(nameof(PlayerState.ShouldWaitRotateFinished), photon.LocalPlayerState.ShouldWaitRotateFinished);
                }
            }
            else
            {
                var playerState = photon.GetByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.bShouldWaitRotateFinished = playerState.ShouldWaitRotateFinished;
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPJumpV2Data), nameof(BUC_ABPJumpV2Data.Update))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchJumpData
    {
        public static void Postfix(
            BUC_ABPJumpV2Data __instance,
            AActor Owner,
            IBUC_ActorBasicData ActorBasicData,
            IBUC_ABPCharacterData ChrData,
            IBUC_ABPBasicData BasicData,
            IBUC_ABPSpecialMoveData SpecialMoveData,
            float DeltaTime)
        {
            if (!(Owner is BGUCharacterCS))
                return;

            var photon = MyMod.Instance.Photon;

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
    [HarmonyPatchCategory(Constants.RoomPatches)]
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
            if (!(Owner is BGUCharacterCS))
                return;

            var photon = MyMod.Instance.Photon;

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
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchTick
    {
        public static void Postfix(float DeltaTime, bool IsThreadTick)
        {
            if (IsThreadTick)
            {
                var photon = MyMod.Instance.Photon;
                photon.SendUpdatedPlayerProperties();

                if (photon.IsMasterClient)
                {
                    photon.SendUpdatedMonsterProperties();
                }
            }
        }
    }

    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public static class PatchAttrs
    {
        public static void Postfix(BUC_AttrContainer __instance)
        {
            var photon = MyMod.Instance.Photon;

            if (photon.IsMasterClient)
            {
                // master client always has the latest data
                return;
            }

            if (__instance.Owner == photon.LocalPlayerState.Pawn)
            {
                // local player (client)
                __instance.SetFloatValue(EBGUAttrFloat.Hp, photon.LocalPlayerState.Hp);
            }
            else
            {
                var playerState = photon.GetByActor(__instance.Owner as BGUCharacterCS);

                // remote player
                if (playerState != null)
                {
                    __instance.SetFloatValue(EBGUAttrFloat.Hp, playerState.Hp);
                }
                else
                {
                    var monster = photon.GetMonsterByCharacter(__instance.Owner as BGUCharacterCS);

                    // monster
                    if (monster != null)
                    {
                        __instance.SetFloatValue(EBGUAttrFloat.Hp, monster.Hp);

                        if (monster.Hp <= 0)
                        {
                            var events = BUS_EventCollectionCS.Get(__instance.Owner);
                            events.Evt_UnitDead.Invoke(__instance.Owner, EDeadReason.SkillDamage); // TODO: Sync other dead reasons?
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public static class PatchHp
    {
        public static bool Prefix(BUS_AttrComp __instance, EBGUAttrFloat AttrID, float NewValue)
        {
            var photon = MyMod.Instance.Photon;

            if (AttrID == EBGUAttrFloat.Hp)
            {
                var owner = __instance.GetOwner();

                // I am a server
                if (photon.IsMasterClient)
                {
                    // I was damaged, set my Hp
                    if (owner == photon.LocalPlayerState.Pawn)
                    {
                        if (photon.LocalPlayerState.Hp.Equals(NewValue, Constants.FloatComparisonTolerance))
                        {
                            photon.LocalPlayerState.Hp = NewValue;
                            photon.SetPlayerProperty(AttrID.ToString(), NewValue);
                        }

                        return true;
                    }

                    // remote player was damaged, set his properties
                    var remotePlayer = MyMod.Instance.Photon.GetByActor(owner);
                    if (remotePlayer != null)
                    {
                        if (remotePlayer.Hp.Equals(NewValue, Constants.FloatComparisonTolerance))
                        {
                            remotePlayer.Hp = NewValue;
                            photon.SendRemotePlayerProperty(remotePlayer.PhotonId, AttrID.ToString(), NewValue);
                        }

                        return true;
                    }

                    // monster was damaged
                    var monster = photon.GetMonsterByCharacter(owner as BGUCharacterCS);
                    if (monster != null)
                    {
                        if (monster.Hp.Equals(NewValue, Constants.FloatComparisonTolerance))
                        {
                            monster.Hp = NewValue;
                            photon.SetMonsterProperty(monster.Id, AttrID.ToString(), NewValue);
                        }

                        return true;
                    }

                    // unsynced monster or sth else
                    return true;
                }

                // I am a client
                return false;
            }

            return true;
        }
    }
}