using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using b1;
using B1UI.GSSvc;
using BtlShare;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using PreludeLib.Attributes;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.NavigationSystem;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.WukongUtils;
using EquipPosition = BtlB1.EquipPosition;

namespace WukongMp.Api.Patches
{
    // runs multithreaded
    [HarmonyPatch(typeof(BUC_ABPBGUCharacterData), nameof(BUC_ABPBGUCharacterData.Update_GameThread))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBGUPlayerAnimation
    {
        public static void Postfix(
            BUC_ABPBGUCharacterData? __instance,
            AActor Owner,
            IBUC_ABPCharacterData ChrData,
            IBUC_SpeedCtrlData SpeedCtrlData,
            float DeltaTime)
        {
            if (!DI.Instance.AreaState.InRoom)
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

            var playerState = DI.Instance.PlayerState;
            var pawnState = DI.Instance.PawnState;
            var mainEntity = playerState.LocalMainCharacter;

            // FIXME: This should be the ownership test
            if (Owner == mainEntity?.GetLocalState().Pawn)
            {
                ref var mainComp = ref mainEntity.Value.GetState();

                if (mainComp.IsStandRotate != __instance.IsStandRotate)
                {
                    mainComp.IsStandRotate = __instance.IsStandRotate;
                }

                if (mainComp.IsAttacking != __instance.IsAttacking)
                {
                    mainComp.IsAttacking = __instance.IsAttacking;
                }

                if (!mainComp.TurnInplaceTargetRotation.ToFRotator().Equals(__instance.TurnInplaceTargetRotation, Constants.FloatComparisonTolerance))
                {
                    mainComp.TurnInplaceTargetRotation = __instance.TurnInplaceTargetRotation.ToVector3();
                }

                if (!mainComp.TurnInplaceRemainAngle.Equals(__instance.TurnInplaceRemainAngle, Constants.FloatComparisonTolerance))
                {
                    mainComp.TurnInplaceRemainAngle = __instance.TurnInplaceRemainAngle;
                }

                if (mainComp.OrientRotationToMovement != __instance.bOrientRotationToMovement)
                {
                    mainComp.OrientRotationToMovement = __instance.bOrientRotationToMovement;
                }
            }
            else
            {
                mainEntity = pawnState.GetEntityByPlayerPawn(Owner);
                if (!mainEntity.HasValue)
                    return;

                ref var mainComp = ref mainEntity.Value.GetState();

                __instance.IsStandRotate = mainComp.IsStandRotate;
                __instance.IsAttacking = mainComp.IsAttacking;
                __instance.TurnInplaceTargetRotation = mainComp.TurnInplaceTargetRotation.ToFRotator();
                __instance.TurnInplaceRemainAngle = mainComp.TurnInplaceRemainAngle;
                __instance.bOrientRotationToMovement = mainComp.OrientRotationToMovement;
            }
        }
    }

    // runs multithreaded
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
            if (!DI.Instance.AreaState.InRoom)
                return;

            if (Owner is not BGUCharacterCS)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var playerState = DI.Instance.PlayerState;

            if (Owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
            {
                var mainEntity = playerState.LocalMainCharacter;
                ref var mainComp = ref mainEntity.Value.GetState();
                if (mainComp.ShouldWaitRotateFinished != __instance.bShouldWaitRotateFinished)
                {
                    mainComp.ShouldWaitRotateFinished = __instance.bShouldWaitRotateFinished;
                }
            }
            else
            {
                var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(Owner);
                if (mainEntity.HasValue)
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    __instance.bShouldWaitRotateFinished = mainComp.ShouldWaitRotateFinished;
                }
                else
                {
                    // maybe it's a monkey summon monster
                    var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(Owner);
                    if (tamerEntity.HasValue)
                    {
                        ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
                        if (!localTamer.IsTamerSynced)
                        {
                            return;
                        }

                        if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                        {
                            ref var anim = ref tamerEntity.Value.GetAnimation();
                            anim.ShouldWaitRotateFinished = __instance.bShouldWaitRotateFinished;
                        }
                        else
                        {
                            ref var anim = ref tamerEntity.Value.GetAnimation();
                            __instance.bShouldWaitRotateFinished = anim.ShouldWaitRotateFinished;
                        }
                    }
                }
            }
        }
    }

    // NOTE: Runs multithreaded
    [HarmonyPatch(typeof(BUC_ABPBasicData), nameof(BUC_ABPBasicData.Update_WorkThread))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBasicData
    {
        public static void Postfix(
            BUC_ABPBasicData __instance,
            AActor Owner)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            if (Owner is not BGUCharacterCS character)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var playerState = DI.Instance.PlayerState;

            if (Owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
            {
                var mainEntity = playerState.LocalMainCharacter;
                ref var mainComp = ref mainEntity.Value.GetState();

                if (mainComp.MoveSpeedLevel != __instance.MoveSpeedLevel.FromGame())
                {
                    mainComp.MoveSpeedLevel = __instance.MoveSpeedLevel.FromGame();
                }

                if (mainComp.MoveSpeedState != __instance.MoveSpeedState.FromGame())
                {
                    mainComp.MoveSpeedState = __instance.MoveSpeedState.FromGame();
                }
            }
            else
            {
                var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(Owner);

                if (mainEntity.HasValue)
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    __instance.MoveSpeedLevel = mainComp.MoveSpeedLevel.ToGame();
                    __instance.MoveSpeedState = mainComp.MoveSpeedState.ToGame();
                }
                else
                {
                    var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(character);

                    if (!tamerEntity.HasValue)
                        return; // unsynced entity

                    ref var anim = ref tamerEntity.Value.GetAnimation();

                    if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                    {
                        anim.MoveSpeedLevel = (byte)__instance.MoveSpeedLevel;
                        anim.MoveSpeedState = (byte)__instance.MoveSpeedState;
                    }
                    else
                    {
                        // apply monster speed data
                        __instance.MoveSpeedLevel = (EMoveSpeedLevel)anim.MoveSpeedLevel;
                        __instance.MoveSpeedState = (EMoveSpeedLevel)anim.MoveSpeedState;
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
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var playerState = DI.Instance.PlayerState;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return false;
            }

            var mainEntity = playerState.LocalMainCharacter;
            if (owner == mainEntity?.GetLocalState().Pawn)
            {
                ref var main = ref mainEntity.Value.GetState();
                main.Equipment = main.Equipment.WithSetItem(EquipPosition.FromGame(), EquipID);
            }

            return owner == GameUtils.GetControlledPawn() || owner.GetName().Contains("Preview") || owner.GetName().Contains("Performer") || owner.GetName().Contains("monkeysummon"); // TODO: Exact comparison
        }
    }

    [HarmonyPatch(typeof(BUS_DeadComp), "OnUnitDead")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitDead
    {
        public static void Prefix(BUS_DeadComp __instance, EDeadReason DeadReason, IBUC_SimpleStateData ___SimpleStateData, IBUC_UnitStateData ___UnitStateData, out bool __state)
        {
            __state = false;

            if (!DI.Instance.AreaState.InRoom)
                return;

            if (DeadReason == EDeadReason.PlayerTrans)
                return;

            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (owner is not BGUCharacterCS ownerCharacter || ___UnitStateData.HasState(EBGUUnitState.Dead) || ___SimpleStateData.HasSimpleState(EBGUSimpleState.PendingDeathInAnimationSyncing))
            {
                return;
            }

            __state = true;
        }

        public static void Postfix(
            BUS_DeadComp __instance,
            bool __state,
            EDeadReason DeadReason,
            AActor Attacker,
            int DmgID = -1,
            int StiffLevel = -1,
            bool bIsDotDmg = false,
            EAbnormalStateType AbnormalType = EAbnormalStateType.None)
        {
            if (!__state)
                return; // skipped prefix

            if (DeadReason == EDeadReason.PlayerTrans)
                return;

            var playerState = DI.Instance.PlayerState;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (owner is not BGUCharacterCS ownerCharacter)
            {
                return;
            }

            if (playerState.LocalMainCharacter.HasValue && owner == playerState.LocalMainCharacter.Value.GetLocalState().Pawn)
            {
                var localMain = playerState.LocalMainCharacter.Value;
                if (!localMain.GetState().IsTransformed)
                {
                    DI.Instance.FreeCameraManager.EnterFreeCameraMode();
                    var netId = localMain.GetMeta().NetId;

                    var payload = new UnitDeadPacket(netId, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
                    localMain.GetState().IsDead = true;
                    DI.Instance.Rpc.SendUnitDead(payload);
                    Logging.LogDebug("Player {PlayerId} died, sending UnitDead event", localMain.GetState().PlayerId);
                }

                return;
            }

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                var meta = tamerEntity.Value.GetMeta();

                var payload = new UnitDeadPacket(meta.NetId, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
                DI.Instance.Rpc.SendUnitDead(payload);
                Logging.LogDebug("Entity {Entity} died, sending UnitDead event", meta.NetId);
            }

            if (Attacker is BGUCharacterCS attackerCharacter &&
                DI.Instance.PawnState.TryGetEnityByCharacter(ownerCharacter, out var victimEntity) &&
                DI.Instance.PawnState.TryGetEnityByCharacter(attackerCharacter, out var attackerEntity))
            {
                DI.Instance.GameplayEventRouter.RaiseOnUnitDead(victimEntity.Value, attackerEntity.Value);
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitTriggerDead
    {
        [HarmonyTargetMethodHint("b1.BUS_UIControlSystemV2", "OnUnitTriggerDead")]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_UIControlSystemV2:OnUnitTriggerDead");
        }

        public static bool Prefix()
        {
            if (!DI.Instance.AreaState.InRoom)
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
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var playerState = DI.Instance.PlayerState;

            var mainEntity = playerState.LocalMainCharacter;
            if (!mainEntity.HasValue)
                return false;

            ref var localMain = ref mainEntity.Value.GetLocalState();

            var localPawn = localMain.Pawn;
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
            if (!DI.Instance.AreaState.InRoom)
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
            if (!DI.Instance.AreaState.InRoom)
                return true;

            if (!DI.Instance.GameplayConfiguration.IsSupportMultiLockEnabled)
            {
                __result = false;
            }

            return DI.Instance.GameplayConfiguration.IsSupportMultiLockEnabled;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchSetTargetToData
    {
        [HarmonyTargetMethodHint("b1.BUS_BattleStateComp", "SetTargetToData")]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BattleStateComp:SetTargetToData");
        }

        public static bool Prefix(UnitLockTargetInfo NewTargetInfo, BUC_TargetInfoData ___TargetInfoData, UActorCompBaseCS __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var playerState = DI.Instance.PlayerState;

            var owner = __instance.GetOwner();
            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return false;
            }

            if (___TargetInfoData.GetTargetInfo()?.LockTargetActor == NewTargetInfo.LockTargetActor)
                return true;

            NetworkId newTargetId = default;
            var clearTarget = true;
            string name = "null (Clear target)";

            var newTargetPlayerEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(NewTargetInfo?.LockTargetActor);
            var newTargetMonsterEntity = DI.Instance.PawnState.GetEntityByTamerMonster(NewTargetInfo?.LockTargetActor);

            if (NewTargetInfo != null && NewTargetInfo.LockTargetActor != null && !newTargetPlayerEntity.HasValue && !newTargetMonsterEntity.HasValue)
            {
                // not synchronized character targeted
                return true;
            }

            if (newTargetPlayerEntity.HasValue)
            {
                newTargetId = newTargetPlayerEntity.Value.GetMeta().NetId;
                name = newTargetPlayerEntity.Value.GetState().CharacterNickName;
                clearTarget = false;
            }
            else if (newTargetMonsterEntity.HasValue)
            {
                newTargetId = newTargetMonsterEntity.Value.GetMeta().NetId;
                name = newTargetMonsterEntity.Value.GetTamer().Guid ?? "Unknown monster";
                clearTarget = false;
            }

            // send only own updates
            if (owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
            {
                var mainEntity = playerState.LocalMainCharacter.Value;

                Logging.LogDebug("New target sent for {Subject} as: {Target}", mainEntity.GetState().CharacterNickName, name);
                DI.Instance.Rpc.SendSetTarget(new TargetData(mainEntity.GetMeta().NetId, newTargetId, clearTarget));
                return true;
            }

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                Logging.LogDebug("New target sent for monster: {Subject} as: {Target}", tamerEntity.Value.GetTamer().Guid ?? "Unknown monster", name);

                var meta = tamerEntity.Value.GetMeta();
                DI.Instance.Rpc.SendSetTarget(new TargetData(meta.NetId, newTargetId, clearTarget));
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerCameraCompImpl), "ApplyCameraControlData")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchApplyCameraControlData
    {
        public static bool Prefix(GSCameraControlData InControlData)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            if (DI.Instance.GameplayConfiguration.EnableCustomCameraArmLength)
            {
                InControlData.ArmLength = Constants.CameraArmLength;
                InControlData.ArmTargetOffset = FVector.ZeroVector;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BUS_BeAttackedComp), "IsDamageValid")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchIsDamageValid
    {
        public static bool Prefix(IBUC_SimpleStateData ___VictimSimpleStateData, ref bool __result)
        {
            if (DI.Instance.GameplayConfiguration.IsStrongDamageImmueEnabled && ___VictimSimpleStateData.HasSimpleState(EBGUSimpleState.StrongDamageImmue))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BUS_ParkourMoveCompImpl), "CheckStrideDown")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchCheckStrideDown
    {
        public static bool Prefix()
        {
            if (!DI.Instance.AreaState.InRoom)
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
            if (!DI.Instance.AreaState.InRoom)
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
            if (!DI.Instance.AreaState.InRoom)
                return true;
            if (DI.Instance.AreaState.CurrentArea == null)
                return true;

            __result = DI.Instance.AreaState.CurrentArea.Value.GetRoom().EnemiesNgPlusLevel + 1;
            return false;
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnTriggerInputActionImpl")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchOnTriggerInputActionImpl
    {
        public static bool Prefix(BUS_PlayerInputActionComp __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var playerState = DI.Instance.PlayerState;

            var playerEntity = playerState.LocalPlayerEntity;
            var mainEntity = playerState.LocalMainCharacter;
            if (!mainEntity.HasValue)
                return true;

            return !(mainEntity.Value.GetLocalState().Pawn == __instance.GetOwner() && mainEntity.Value.GetPvP().IsSpectator);
        }
    }

    // Disable slowing down time
    [HarmonyPatch(typeof(BUS_TimeScaleComp), "OnTriggerScaleTime")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchOnTriggerScaleTime
    {
        public static bool Prefix()
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchSetAllUnitCannotDead
    {
        [HarmonyTargetMethodHint("b1.BIS_DeathManager", "SetAllUnitCannotDead")]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BIS_DeathManager:SetAllUnitCannotDead");
        }

        public static bool Prefix(bool bInCanUnitDead)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            return !bInCanUnitDead;
        }
    }

    [HarmonyPatch(typeof(BUS_QuestDynamicObstacleComp), "EnableCollision")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchEnableCollision
    {
        public static bool Prefix(BUS_QuestDynamicObstacleComp __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var obstacle = __instance.GetOwner();

            List<FVector> playersPositions = [];
            DI.Instance.World.Query<MainCharacterComponent>().ForEachEntity((
                ref playerComp, _) =>
            {
                playersPositions.Add(playerComp.Location.ToFVector());
            });

            if (playersPositions.Count <= 1)
                return true;

            var enableCollider = true;
            for (int i = 1; i < playersPositions.Count; i++)
            {
                var nav = UNavigationSystemV1.FindPathToLocationSynchronously(obstacle.World, playersPositions[0], playersPositions[i], null, null);
                var path = nav.PathPoints.ToList();
                if (IsPathNearPosition(path, obstacle.GetActorLocation(), Constants.ArenaPortalRadius))
                {
                    enableCollider = false;
                    break;
                }
            }

            Logging.LogDebug("{Status} collider with guid {Guid}", enableCollider ? "Enabling" : "Disabling", BGU_DataUtil.GetActorGuid(obstacle));
            return enableCollider;
        }

        private static bool IsPathNearPosition(IList<FVector> pathPoints, FVector worldPos, float radius)
        {
            if (pathPoints == null || pathPoints.Count == 0 || radius <= 0f)
                return false;

            float radiusSquared = radius * radius;

            FVector ClosestPointOnSegment(FVector segmentStart, FVector segmentEnd, FVector point)
            {
                FVector segmentVector = segmentEnd - segmentStart;
                double segmentLength = segmentVector.SizeSquared();
                if (segmentLength <= 1e-6f) return segmentStart;
                double t = FVector.DotProduct(point - segmentStart, segmentVector) / segmentLength;
                t = FMath.Clamp(t, 0, 1);
                return segmentStart + t * segmentVector;
            }

            for (int i = 0; i < pathPoints.Count; i++)
            {
                if (FVector.DistSquared2D(pathPoints[i], worldPos) <= radiusSquared)
                    return true;
            }

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                FVector closest = ClosestPointOnSegment(pathPoints[i], pathPoints[i + 1], worldPos);
                if (FVector.DistSquared2D(closest, worldPos) <= radiusSquared)
                    return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractStepMatchPos), "StepBegin")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnInteractStepBegin
    {
        public static void Prefix(InteractStepMatchPos __instance, InteractContext ___Context)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            var character = ___Context.OwnerController.GetControlledPawn();
            var localMainEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (!localMainEntity.HasValue)
                return;

            ref var localMainComp = ref localMainEntity.Value.GetLocalState();
            if (localMainComp.Pawn != character)
                return;

            Logging.LogDebug("InteractStepMatchPos started, disabling collision for all players");
            foreach (var playerId in DI.Instance.State.OtherAreaPlayers)
            {
                var mainEntity = DI.Instance.PlayerState.GetMainCharacterById(playerId);
                if (mainEntity == null)
                    continue;
                ref var localMain = ref mainEntity.Value.GetLocalState();
                if (localMain.Pawn == null)
                    continue;
                PlayerUtils.EnablePlayerPawnCollision(localMain.Pawn, false);
            }
        }
    }

    [HarmonyPatch(typeof(InteractStepMatchPos), "StepFinish")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnInteractStepFinish
    {
        public static void Prefix(InteractStepMatchPos __instance, InteractContext ___Context)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            var character = ___Context.OwnerController.GetControlledPawn();
            var localMainEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (!localMainEntity.HasValue)
                return;

            ref var localMainComp = ref localMainEntity.Value.GetLocalState();
            if (localMainComp.Pawn != character)
                return;

            Logging.LogDebug("InteractStepMatchPos finished, enabling collision for all players");
            foreach (var playerId in DI.Instance.State.OtherAreaPlayers)
            {
                var mainEntity = DI.Instance.PlayerState.GetMainCharacterById(playerId);
                if (mainEntity == null)
                    continue;
                ref var localMain = ref mainEntity.Value.GetLocalState();
                if (localMain.Pawn == null)
                    continue;
                PlayerUtils.EnablePlayerPawnCollision(localMain.Pawn, true);
            }
        }
    }

    [HarmonyPatch(typeof(InteractStepMatchPos), "OnTick")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchInteractStepMatchPosOnTick
    {
        public static Exception? Finalizer(Exception? __exception)
        {
            if (__exception != null)
            {
                DI.Instance.Logger.LogError(__exception, "Exception in InteractStepMatchPos.OnTick");
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(B1BattleLogicSvc), "RebirthPointRest")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnRebirthPointRest
    {
        public static bool Prefix(InteractStepMatchPos __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            BPC_RebirthPointData rebirthPointData = BGU_DataUtil.GetReadOnlyData<BPC_RebirthPointData>(GameUtils.GetPlayerController());
            DI.Instance.Rpc.SendRestAtShrine(rebirthPointData.CurrentBirthPoint.PointID);

            return true;
        }
    }

    [HarmonyPatch(typeof(BUC_ABPMotionMatchingData), "UpdatePlayerMotionMatchingState")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchUpdatePlayerMotionMatchingState
    {
        public static bool Prefix(
            BUC_ABPMotionMatchingData __instance,
            AActor Owner,
            IBUC_TargetInfoData ___TargetInfoData,
            IBUC_UnitStateData ___UnitStateData,
            IBUC_PlayerCameraData ___CameraData,
            EMoveSpeedLevel ___MMMoveSpeedState)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            if (Owner == null)
            {
                return false;
            }

            ACharacter? aCharacter = Owner as ACharacter;
            if (aCharacter == null || aCharacter is not BGUPlayerCharacterCS)
            {
                return false;
            }

            bool flag = false;
            if (___TargetInfoData != null)
            {
                UnitLockTargetInfo targetInfo = ___TargetInfoData.GetTargetInfo();
                if (targetInfo != null && targetInfo.LockTargetActor != null && targetInfo.LockTargetWayType == ELockTargetWayType.Manual)
                {
                    flag = true;
                }
            }

            if (___UnitStateData != null && ___UnitStateData.HasState(EBGUUnitState.ShooterMode))
            {
                flag = true;
            }

            if (___CameraData != null && ___CameraData.IsInG4Mode())
            {
                flag = true;
            }

            switch (___MMMoveSpeedState)
            {
                case EMoveSpeedLevel.Walk:
                    __instance.TargetMMState = (flag ? EState_MM.LockWalk : EState_MM.FreeWalk);
                    break;
                case EMoveSpeedLevel.Run:
                    __instance.TargetMMState = (flag ? EState_MM.LockRun : EState_MM.FreeRun);
                    break;
                case EMoveSpeedLevel.Sprint:
                    __instance.TargetMMState = (flag ? EState_MM.LockSprint : EState_MM.FreeSprint);
                    break;
                default:
                    __instance.TargetMMState = (flag ? EState_MM.Lock : EState_MM.Free);
                    break;
            }

            return false;
        }
    }
}

[HarmonyPatch(typeof(BUS_JumpComp), "TriggerJumpSkill", typeof(ESkillDirection), typeof(FVector2D))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTriggerJumpSkill
{
    public static void Prefix(BUS_JumpComp __instance, ESkillDirection StartJumpDir, FVector2D CurrentInputVector)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();
        var playerState = DI.Instance.PlayerState;

        if (owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
        {
            DI.Instance.Rpc.SendStartJump(new StartJumpData(StartJumpDir, CurrentInputVector));
        }
    }
}

[HarmonyPatch(typeof(BUS_JumpComp), "OnReleased")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchJumpOnReleased
{
    public static void Prefix(BUS_JumpComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();
        var playerState = DI.Instance.PlayerState;

        if (owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
        {
            DI.Instance.Rpc.SendStopJump();
        }
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "CheckCanSelectTarget")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchCheckCanSelectTarget
{
    public static bool Prefix(AActor Player, ref bool __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var actor = Player as ACharacter;
        if (actor != null && actor.GetController() == null)
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerWukongAttrDataInit), nameof(PlayerWukongAttrDataInit.SetAttrTransAfterActiveTalent))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchSetAttrTransAfterActiveTalent
{
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            DI.Instance.Logger.LogError(__exception, "Suppressed crash in SetAttrTransAfterActiveTalent");
        }

        return null;
    }
}

[HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnSetRebirthPointAsCurrentBirthPoint")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnSetRebirthPointAsCurrentBirthPoint
{
    public static void Postfix(UActorCompBaseCS __instance, int RebirthPointID)
    {
        PlayerUtils.LogRebirthPointChange(__instance.GetOwner(), RebirthPointID);
        var owner = __instance.GetOwner();
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEnityByCharacter(character, out var entity))
        {
            DI.Instance.GameplayEventRouter.RaiseOnRebirthPointChanged(entity.Value, RebirthPointID);
        }
    }
}

[HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnSetCurrentBirthPoint")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnSetCurrentBirthPoint
{
    public static void Postfix(UActorCompBaseCS __instance, int BirthPointID)
    {
        PlayerUtils.LogRebirthPointChange(__instance.GetOwner(), BirthPointID);
    }
}

[HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnForceSetRebirthPoint")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnForceSetRebirthPoint
{
    public static void Postfix(UActorCompBaseCS __instance, int RebirthPointId)
    {
        PlayerUtils.LogRebirthPointChange(__instance.GetOwner(), RebirthPointId);
        var owner = __instance.GetOwner();
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEnityByCharacter(character, out var entity))
        {
            DI.Instance.GameplayEventRouter.RaiseOnRebirthPointChanged(entity.Value, RebirthPointId);
        }
    }
}