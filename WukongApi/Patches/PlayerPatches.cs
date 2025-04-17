using System.Reflection;
using b1;
using B1UI.GSUI;
using BtlB1;
using BtlShare;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.State;

namespace WukongApi.Patches
{
    [HarmonyPatch(typeof(BUC_ABPBGUCharacterData), nameof(BUC_ABPBGUCharacterData.Update_GameThread))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBGUPlayerAnimation
    {
        public static void Postfix(
            BUC_ABPBGUCharacterData __instance,
            AActor Owner,
            IBUC_ABPCharacterData ChrData,
            IBUC_SpeedCtrlData SpeedCtrlData,
            float DeltaTime)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (__instance == null)
            {
                Logging.LogError("__instance is null in BUC_ABPBGUCharacterData.Update_GameThread");
                return;
            }

            if (Owner is not BGUCharacterCS)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var client = WukongMP.Instance.Client;

            if (Owner == client.LocalPlayerState.Pawn)
            {
                var localState = client.LocalPlayerState;

                if (localState.IsStandRotate != __instance.IsStandRotate)
                {
                    client.LocalPlayerState.IsStandRotate = __instance.IsStandRotate;
                    client.CachePlayerProperty(nameof(PlayerState.IsStandRotate), client.LocalPlayerState.IsStandRotate);
                }

                if (localState.IsAttacking != __instance.IsAttacking)
                {
                    client.LocalPlayerState.IsAttacking = __instance.IsAttacking;
                    client.CachePlayerProperty(nameof(PlayerState.IsAttacking), client.LocalPlayerState.IsAttacking);
                }

                if (!localState.TurnInplaceTargetRotation.Equals(__instance.TurnInplaceTargetRotation, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.TurnInplaceTargetRotation = __instance.TurnInplaceTargetRotation;
                    client.CachePlayerProperty(nameof(PlayerState.TurnInplaceTargetRotation), client.LocalPlayerState.TurnInplaceTargetRotation);
                }

                if (!localState.TurnInplaceRemainAngle.Equals(__instance.TurnInplaceRemainAngle, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.TurnInplaceRemainAngle = __instance.TurnInplaceRemainAngle;
                    client.CachePlayerProperty(nameof(PlayerState.TurnInplaceRemainAngle), client.LocalPlayerState.TurnInplaceRemainAngle);
                }

                if (localState.OrientRotationToMovement != __instance.bOrientRotationToMovement)
                {
                    client.LocalPlayerState.OrientRotationToMovement = __instance.bOrientRotationToMovement;
                    client.CachePlayerProperty(nameof(PlayerState.OrientRotationToMovement), client.LocalPlayerState.OrientRotationToMovement);
                }
            }
            else
            {
                var playerState = client.GetPlayerByActor(Owner);

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
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (Owner is not BGUCharacterCS)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var client = WukongMP.Instance.Client;

            if (Owner == client.LocalPlayerState.Pawn)
            {
                var localState = client.LocalPlayerState;

                if (localState.ShouldWaitRotateFinished != __instance.bShouldWaitRotateFinished)
                {
                    client.LocalPlayerState.ShouldWaitRotateFinished = __instance.bShouldWaitRotateFinished;
                    client.CachePlayerProperty(nameof(PlayerState.ShouldWaitRotateFinished), client.LocalPlayerState.ShouldWaitRotateFinished);
                }
            }
            else
            {
                var playerState = client.GetPlayerByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.bShouldWaitRotateFinished = playerState.ShouldWaitRotateFinished;
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPJumpV2Data), nameof(BUC_ABPJumpV2Data.Update))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (Owner is not BGUCharacterCS)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var client = WukongMP.Instance.Client;

            if (Owner == client.LocalPlayerState.Pawn)
            {
                var localState = client.LocalPlayerState;

                if (localState.InJump != __instance.bInJump)
                {
                    client.LocalPlayerState.InJump = __instance.bInJump;
                    client.CachePlayerProperty(nameof(PlayerState.InJump), client.LocalPlayerState.InJump);
                }
            }
            else
            {
                var playerState = client.GetPlayerByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.bInJump = playerState.InJump;
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPBasicData), nameof(BUC_ABPBasicData.Update_WorkThread))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (Owner is not BGUCharacterCS character)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var client = WukongMP.Instance.Client;

            if (Owner == client.LocalPlayerState.Pawn)
            {
                var localState = client.LocalPlayerState;

                if (localState.MoveSpeedLevel != __instance.MoveSpeedLevel)
                {
                    client.LocalPlayerState.MoveSpeedLevel = __instance.MoveSpeedLevel;
                    client.CachePlayerProperty(nameof(PlayerState.MoveSpeedLevel), client.LocalPlayerState.MoveSpeedLevel);
                }

                if (localState.MoveSpeedState != __instance.MoveSpeedState)
                {
                    client.LocalPlayerState.MoveSpeedState = __instance.MoveSpeedState;
                    client.CachePlayerProperty(nameof(PlayerState.MoveSpeedState), client.LocalPlayerState.MoveSpeedState);
                }
            }
            else
            {
                var playerState = client.GetPlayerByActor(Owner);

                if (playerState != null)
                {
                    __instance.MoveSpeedLevel = playerState.MoveSpeedLevel;
                    __instance.MoveSpeedState = playerState.MoveSpeedState;
                }
                else
                {
                    var monsterState = client.GetMonsterByCharacter(character);

                    if (monsterState == null)
                        return; // unsynced entity

                    if (client.IsMasterClient)
                    {
                        // send monster speed data
                        if (monsterState.MoveSpeedLevel != __instance.MoveSpeedLevel)
                        {
                            monsterState.MoveSpeedLevel = __instance.MoveSpeedLevel;
                            client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveSpeedLevel), monsterState.MoveSpeedLevel);
                        }

                        if (monsterState.MoveSpeedState != __instance.MoveSpeedState)
                        {
                            monsterState.MoveSpeedState = __instance.MoveSpeedState;
                            client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveSpeedState), monsterState.MoveSpeedState);
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

    [HarmonyPatch(typeof(BUS_EquipComp), "OnChangeEquip")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchEqCompUpdate
    {
        public static bool Prefix(BUS_EquipComp __instance, EquipPosition EquipPosition, int EquipID)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMP.Instance.Client;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return false;
            }

            if (owner == client.LocalPlayerState.Pawn)
            {
                client.CacheEquipmentChange(EquipPosition, EquipID);
            }

            return owner == client.LocalPlayerState.Pawn || owner.GetName().Contains("Preview"); // TODO: Exact comparison
        }
    }

    [HarmonyPatch(typeof(BUS_DeadComp), "OnUnitDead")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitDead
    {
        public static void Prefix(BUS_DeadComp __instance, EDeadReason DeadReason, AActor Attacker, IBUC_SimpleStateData ___SimpleStateData, IBUC_UnitStateData ___UnitStateData)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (DeadReason == EDeadReason.PlayerTrans)
                return; // TODO: Camera is broken after transformation, stuck in one direction

            var client = WukongMP.Instance.Client;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var bGUCharacterCS = owner as BGUCharacterCS;
            if (bGUCharacterCS == null || ___UnitStateData.HasState(EBGUUnitState.Dead) || ___SimpleStateData.HasSimpleState(EBGUSimpleState.PendingDeathInAnimationSyncing))
            {
                return;
            }

            if (client is { IsMasterClient: true, RoomState.InPvP: true, RoomState.InCombatRound: true })
            {
                if (Attacker != owner)
                {
                    var attackerPlayerState = client.GetPlayerByActor(Attacker);
                    var killedPlayerState = client.GetPlayerByActor(owner);
                    if (attackerPlayerState != null && killedPlayerState != null)
                    {
                        client.WukongChat.SendServerMessage($"{attackerPlayerState.NickName} killed {killedPlayerState.NickName}");
                    }
                }

                client.CheckRoundEndCondition();
            }
        }

        public static void Postfix(BUS_DeadComp __instance, EDeadReason DeadReason, AActor Attacker)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (DeadReason == EDeadReason.PlayerTrans || DeadReason == EDeadReason.OnlyDestroyUnit)
                return; // TODO: Camera is broken after transformation, stuck in one direction

            var client = WukongMP.Instance.Client;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (owner == client.LocalPlayerState.Pawn)
            {
                WukongMP.Instance.FreeCameraManager.EnterFreeCameraMode();
            }
        }
    }

    [HarmonyPatch(typeof(UIDeath), "DoShowIn")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchUIDeath
    {
        public static bool Prefix()
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerCameraCompImpl), "OnTickWithGroup")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchCameraCompTick
    {
        public static bool Prefix(BUS_PlayerCameraCompImpl __instance)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMP.Instance.Client;

            var localPawn = client.LocalPlayerState.Pawn;
            var owner = __instance.GetOwner();
            
            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return false;
            }

            if (owner == localPawn)
            {
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(BUS_FallingCompl), "SafeFallingTimerTick")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchFallDamage
    {
        public static bool Prefix()
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(BUC_TargetInfoData), "IsSupportMultiLockTarget")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchIsSupportMultiLockTarget
    {
        public static bool Prefix(ref bool __result)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchSetTargetToData
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BattleStateComp:SetTargetToData");
        }

        public static void Prefix(UnitLockTargetInfo NewTargetInfo, BUC_TargetInfoData ___TargetInfoData, UActorCompBaseCS __instance)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;
            
            var owner = __instance.GetOwner();
            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            // send only own updates
            if (owner != client.LocalPlayerState.Pawn)
                return;

            if (___TargetInfoData.GetTargetInfo()?.LockTargetActor == NewTargetInfo.LockTargetActor)
                return;

            var newTargetCharacterState = client.GetCharacterByActor(NewTargetInfo?.LockTargetActor);
            if (newTargetCharacterState  != null)
            {
                Logging.LogDebug("New target sent for {Subject} as: {Target}", client.LocalPlayerState.NickName, newTargetCharacterState.NickName);
                client.SendTarget(newTargetCharacterState.PeerId);
            }
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerCameraCompImpl), "ApplyCameraControlData")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchApplyCameraControlData
    {
        public static bool Prefix(GSCameraControlData InControlData)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            InControlData.ArmLength = Constants.CameraArmLength;
            InControlData.ArmTargetOffset = FVector.ZeroVector;
            return true;
        }
    }

    [HarmonyPatch(typeof(BUS_BeAttackedComp), "DoDamageLogic")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchDoDamageLogic
    {
        public static void Postfix(BUS_BeAttackedComp __instance, AActor Attacker)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;
            if (client.IsMasterClient)
            {
                var owner = __instance.GetOwner();
                
                if (owner.IsNullOrDestroyed())
                {
                    Logging.LogError("Owner is null or destroyed");
                    return;
                }
                
                var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(owner);
                var hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);

                // Manually trigger UnitDead
                if (hp <= 0)
                {
                    var events = BUS_EventCollectionCS.Get(owner);
                    GameLoopPatch.QueueOnGameThread(() => { events.Evt_UnitDead.Invoke(Attacker, EDeadReason.SkillDamage); }, "Evt_UnitDead");
                }
            }
        }
    }

    [HarmonyPatch(typeof(BUS_ParkourMoveCompImpl), "CheckStrideDown")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchCheckStrideDown
    {
        public static bool Prefix()
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(BGW_GameDB), "GetUnitBattleInfoExtendDesc")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchGetUnitBattleInfoExtendDesc
    {
        public static void Postfix(ref FUStUnitBattleInfoExtendDesc? __result)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (__result != null && __result.DefaultCamID == 0)
                __result.DefaultCamID = 101600;
        }
    }
    
    [HarmonyPatch(typeof(BPC_PlayerRoleData), "GetNewGamePlusCount")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchGetNewGamePlusCount
    {
        public static bool Prefix(ref int __result)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMP.Instance.Client;
            if (client.RoomState.EnemiesNgPlusLevel == 0)
            {
                return true;
            }

            __result = client.RoomState.EnemiesNgPlusLevel;
            return false;
        }
    }
}