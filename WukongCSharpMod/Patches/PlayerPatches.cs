using b1;
using BtlB1;
using HarmonyLib;
using UnrealEngine.Engine;
using WukongCSharpMod.State;

namespace WukongCSharpMod.Patches
{
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
            if (__instance == null)
            {
                Helpers.LogError("__instance is null in BUC_ABPBGUCharacterData.Update_GameThread");
                return;
            }

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

                if (!localState.TurnInplaceRemainAngle.Equals(__instance.TurnInplaceRemainAngle, Constants.FloatComparisonTolerance))
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
            if (!(Owner is BGUCharacterCS character))
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

                if (playerState != null)
                {
                    __instance.MoveSpeedLevel = playerState.MoveSpeedLevel;
                    __instance.MoveSpeedState = playerState.MoveSpeedState;
                }
                else
                {
                    var monsterState = photon.GetMonsterByCharacter(character);

                    if (monsterState == null)
                        return; // unsynced entity

                    if (photon.IsMasterClient)
                    {
                        // send monster speed data
                        if (monsterState.MoveSpeedLevel != __instance.MoveSpeedLevel)
                        {
                            monsterState.MoveSpeedLevel = __instance.MoveSpeedLevel;
                            photon.SetMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveSpeedLevel), monsterState.MoveSpeedLevel);
                        }

                        if (monsterState.MoveSpeedState != __instance.MoveSpeedState)
                        {
                            monsterState.MoveSpeedState = __instance.MoveSpeedState;
                            photon.SetMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveSpeedState), monsterState.MoveSpeedState);
                        }
                    }
                    else
                    {
                        // apply monster speed data
                        __instance.MoveSpeedLevel = monsterState.MoveSpeedLevel;
                        __instance.MoveSpeedState = monsterState.MoveSpeedState;
                    }
                }
            }
        }
    }

    // [HarmonyPatch(typeof(BUS_CharacterModularCompImpl), "RefreshSkeletalMesh")]
    // [HarmonyPatchCategory(Constants.RoomPatches)]
    // public class PatchEqMeshChange
    // {
    //     public static bool Prefix(BUS_CharacterModularCompImpl __instance)
    //     {
    //         var photon = MyMod.Instance.Photon;
    //         var owner = __instance.GetOwner();
    //
    //         return owner.GetName() == "Unit_EquipPreview_Wukong_C_2" || owner == photon.LocalPlayerState.Pawn;
    //     }
    // }

    [HarmonyPatch(typeof(BUS_EquipComp), "OnChangeEquip")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchEqCompUpdate
    {
        public static bool Prefix(BUS_EquipComp __instance, EquipPosition Position, int EquipID)
        {
            var photon = MyMod.Instance.Photon;
            var owner = __instance.GetOwner();

            if (owner != photon.LocalPlayerState.Pawn)
                return false;

            photon.SendEqChange(Position, EquipID);
            return true;
        }
    }
}