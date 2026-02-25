using System;
using System.Collections.Generic;
using System.Reflection;
using b1;
using B1UI.GSSvc;
using BtlShare;
using Friflo.Engine.ECS;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using PreludeLib.Attributes;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;
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
            if (Owner == mainEntity?.Pawn)
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
                mainEntity = pawnState.GetEntityByPlayerActor(Owner);
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

            if (Owner == playerState.LocalMainCharacter?.Pawn)
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
                var mainEntity = DI.Instance.PawnState.GetEntityByPlayerActor(Owner);
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

                        if (DI.Instance.ClientOwnership_.OwnsEntity(tamerEntity.Value.Entity))
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

            if (Owner == playerState.LocalMainCharacter?.Pawn)
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
                var mainEntity = DI.Instance.PawnState.GetEntityByPlayerActor(Owner);

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

                    if (DI.Instance.ClientOwnership_.OwnsEntity(tamerEntity.Value.Entity))
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
            if (owner == mainEntity?.Pawn)
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

            if (playerState.LocalMainCharacter.HasValue && owner == playerState.LocalMainCharacter.Value.Pawn)
            {
                var localMain = playerState.LocalMainCharacter.Value;
                if (!localMain.GetState().IsTransformed)
                {
                    ref var localState = ref localMain.GetLocalState();
                    localState.IsDuringDeathAnim = true;
                    IBPC_BattleMainInfoData battleData = BGU_DataUtil.GetReadOnlyData<IBPC_BattleMainInfoData, BPC_BattleMainInfoData>(localMain.Pawn?.GetController());
                    if (battleData != null)
                    {
                        localState.DeadAnimationTime = battleData.PlayerDeathUIDelayTime;
                    }

                    localState.DeadAnimationTime = 6f; // Value from game.

                    var @event = new UnitDeadEvent(localMain, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
                    localMain.GetState().IsDead = true;
                    DI.Instance.MappedEvent.PropagateToEcs(@event);
                    Logging.LogDebug("Player {PlayerId} died, sending UnitDead event", localMain.GetState().PlayerId);
                }

                return;
            }

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership_.OwnsEntity(tamerEntity.Value.Entity))
            {
                var meta = tamerEntity.Value.GetMeta();
                var payload = new UnitDeadEvent(tamerEntity.Value, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
                DI.Instance.MappedEvent.PropagateToEcs(payload);
                Logging.LogDebug("Entity {Entity} died, sending UnitDead event", meta.NetId);
            }

            if (Attacker is BGUCharacterCS attackerCharacter &&
                DI.Instance.PawnState.TryGetEntityByCharacter(ownerCharacter, out var victimEntity) &&
                DI.Instance.PawnState.TryGetEntityByCharacter(attackerCharacter, out var attackerEntity))
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

            var localPawn = mainEntity.Value.Pawn;
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

            Entity newTarget = default;
            var clearTarget = true;
            string name = "null (Clear target)";

            var newTargetPlayerEntity = DI.Instance.PawnState.GetEntityByPlayerActor(NewTargetInfo?.LockTargetActor);
            var newTargetMonsterEntity = DI.Instance.PawnState.GetEntityByTamerMonster(NewTargetInfo?.LockTargetActor);

            if (NewTargetInfo != null && NewTargetInfo.LockTargetActor != null && !newTargetPlayerEntity.HasValue && !newTargetMonsterEntity.HasValue)
            {
                // not synchronized character targeted
                return true;
            }

            if (newTargetPlayerEntity.HasValue)
            {
                newTarget = newTargetPlayerEntity.Value;
                name = newTargetPlayerEntity.Value.GetState().CharacterNickName;
                clearTarget = false;
            }
            else if (newTargetMonsterEntity.HasValue)
            {
                newTarget = newTargetMonsterEntity.Value;
                name = newTargetMonsterEntity.Value.GetTamer().Guid ?? "Unknown monster";
                clearTarget = false;
            }

            // send only own updates
            if (owner == playerState.LocalMainCharacter?.Pawn)
            {
                var mainEntity = playerState.LocalMainCharacter.Value;

                Logging.LogDebug("New target sent for {Subject} as: {Target}", mainEntity.GetState().CharacterNickName, name);
                DI.Instance.MappedEvent.PropagateToEcs(new SetTargetEvent(mainEntity, newTarget, clearTarget));
                return true;
            }

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership_.OwnsEntity(tamerEntity.Value.Entity))
            {
                Logging.LogDebug("New target sent for monster: {Subject} as: {Target}", tamerEntity.Value.GetTamer().Guid ?? "Unknown monster", name);

                DI.Instance.MappedEvent.PropagateToEcs(new SetTargetEvent(tamerEntity.Value, newTarget, clearTarget));
                return true;
            }

            return false;
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
                var isTransformed = DI.Instance.PlayerState.LocalMainCharacter?.GetState().IsTransformed ?? false;

                InControlData.ArmLength = Math.Max(InControlData.ArmLength, isTransformed ? Constants.TransformedCameraArmLength : Constants.CameraArmLength);
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

    [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnTriggerInputActionImpl")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchOnTriggerInputActionImpl
    {
        public static bool Prefix(BUS_PlayerInputActionComp __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var playerState = DI.Instance.PlayerState;

            var mainEntity = playerState.LocalMainCharacter;
            if (!mainEntity.HasValue)
                return true;

            return !(mainEntity.Value.Pawn == __instance.GetOwner() && mainEntity.Value.GetPvP().IsSpectator);
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

    // TODO: Replaced with a system in co-op, check if it works
    // [HarmonyPatch(typeof(BUS_QuestDynamicObstacleComp), "DisableCollision")]
    // [HarmonyPatchCategory(Constants.ConnectedPatches)]
    // public class PatchDisableCollision
    // {
    //     public static void Postfix(BUS_QuestDynamicObstacleComp __instance)
    //     {
    //         if (!DI.Instance.AreaState.InRoom)
    //             return;
    //
    //         var obstacle = __instance.GetOwner();
    //         DI.Instance.ColliderDisableData.PermanentlyDisableCollider(obstacle);
    //     }
    // }

    // [HarmonyPatch(typeof(BUS_TouchWallFeedbackComp), "CheckCanTrigger_HitDynamicObstacleWall")]
    // [HarmonyPatchCategory(Constants.ConnectedPatches)]
    // public class PatchCheckCanTrigger_HitDynamicObstacleWall
    // {
    //     public static bool Prefix(BUS_TouchWallFeedbackComp __instance, AActor HitActor)
    //     {
    //         if (!DI.Instance.AreaState.InRoom)
    //             return true;
    //
    //         BGU_QuestActor? questActor = HitActor as BGU_QuestActor;
    //         if (questActor == null)
    //             return true;
    //
    //         if (questActor.QuestActorType != EQuestActorType.DynamicObstacle)
    //             return true;
    //
    //         var player = __instance.GetOwner() as BGUPlayerCharacterCS;
    //         if (player == null)
    //             return true;
    //
    //         if (player != DI.Instance.PlayerState.LocalMainCharacter?.Pawn)
    //             return true;
    //
    //         var bossActor = GetClosestBossActor(player, player.GetActorLocation());
    //         if (bossActor == null)
    //             return true;
    //
    //         var bossLocation = bossActor.GetActorLocation();
    //         if (UBGUSelectUtil.MultiSphereTraceForObjects(player, player.GetActorLocation(), bossLocation, 10, [EObjectTypeQuery.ObjectTypeQuery15], false, out var HitResult) > 0 && HitResult.Any(x => x.HitActor == HitActor))
    //         {
    //             Logging.LogDebug("Hit dynamic obstacle wall is between boss and player, disabling collision temporarily");
    //             DI.Instance.ColliderDisableData.DisableCollider(questActor, Constants.ColliderDisableTime);
    //             return false;
    //         }
    //
    //         return true;
    //     }

    //     private static AActor? GetClosestBossActor(UObject context, FVector position)
    //     {
    //         AActor? closestBoss = null;
    //         var closestDistanceSquared = double.MaxValue;
    //         var monsters = UGameplayStatics.GetAllActorsOfClass<BGU_CharacterAI?>(context);
    //         foreach (var monster in monsters)
    //         {
    //             if (monster.IsNullOrDestroyed())
    //                 continue;
    //
    //             var info = BGW_GameDB.GetUnitBattleInfoExtendDesc(monster.GetFinalBattleInfoExtendID());
    //
    //             if (info == null)
    //                 continue;
    //
    //             if (!(monster.bBossRoomMonster || info.QualityType is EUnitQualityType.NormalBoss or EUnitQualityType.FinalBoss || info.BloodBarType == EBGUBloodBarType.BossBar))
    //                 continue;
    //
    //             var distanceSquared = FVector.DistSquared2D(monster.GetActorLocation(), position);
    //             if (distanceSquared < closestDistanceSquared)
    //             {
    //                 closestDistanceSquared = distanceSquared;
    //                 closestBoss = monster;
    //             }
    //         }
    //
    //         return closestBoss;
    //     }
    // }

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

            if (localMainEntity.Value.Pawn != character)
                return;

            Logging.LogWarning("InteractStepMatchPos started, disabling collision for all players");
            PlayerUtils.DisableOtherPlayersCollision(DI.Instance.State, DI.Instance.PlayerState);
        }
    }

    [HarmonyPatch(typeof(InteractStepBase), "TriggerFinish")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchTriggerFinish
    {
        public static void Prefix(InteractStepBase __instance, InteractContext ___Context)
        {
            if (__instance is not InteractStepMatchPos)
                return;

            if (!DI.Instance.AreaState.InRoom)
                return;

            var character = ___Context.OwnerController.GetControlledPawn();
            var localMainEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (!localMainEntity.HasValue)
                return;

            if (localMainEntity.Value.Pawn != character)
                return;

            Logging.LogWarning("InteractStepMatchPos finished, enabling collision for all players");
            PlayerUtils.AllowOtherPlayersCollision(DI.Instance.State, DI.Instance.PlayerState);
        }
    }

    [HarmonyPatch(typeof(BGS_GameBgmMgr), "OnUIShrineMainActive")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUIShrineMainActive
    {
        public static void Postfix(bool IsActive)
        {
            if (IsActive)
            {
                Logging.LogWarning("OnUIShrineMainActive is active, disabling collision for all players");
                PlayerUtils.DisableOtherPlayersCollision(DI.Instance.State, DI.Instance.PlayerState);
            }
            else
            {
                Logging.LogWarning("OnUIShrineMainActive is not active, enabling collision for all players");
                PlayerUtils.AllowOtherPlayersCollision(DI.Instance.State, DI.Instance.PlayerState);
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
        public static bool Prefix()
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var playerController = GameUtils.GetPlayerController()!;
            var owner = playerController.GetControlledPawn();

            if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped_(owner, out var mainEntity))
            {
                var rebirthPointData = BGU_DataUtil.GetReadOnlyData<BPC_RebirthPointData>(GameUtils.GetPlayerController());
                DI.Instance.MappedEvent.PropagateToEcs(new RestAtShrineEvent(mainEntity.Value, rebirthPointData.CurrentBirthPoint.PointID));
            }

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

        if (owner == playerState.LocalMainCharacter?.Pawn)
        {
            DI.Instance.MappedEvent.PropagateToEcs(new StartJumpEvent(playerState.LocalMainCharacter.Value, StartJumpDir, CurrentInputVector));
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

        if (owner == playerState.LocalMainCharacter?.Pawn)
        {
            DI.Instance.MappedEvent.PropagateToEcs(new StopJumpEvent(playerState.LocalMainCharacter.Value));
        }
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "CheckCanSelectTarget")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchCheckCanSelectTarget
{
    public static bool Prefix(AActor Player, string Socket, ref bool __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var actor = Player as ACharacter;
        if (actor == null)
            return true;

        if (actor.GetController() == null)
        {
            __result = false;
            return false;
        }

        if (actor is BGUPlayerCharacterCS && (
                Socket == Constants.FeetCameraLockNode || // do not lock on Wukong's feet
                BGUFunctionLibraryCS.BGUHasUnitSimpleState(actor, EBGUSimpleState.PhantomRush))) // do not lock on Phantom rushed players
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
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEntityByCharacter(character, out var entity))
        {
            entity.Value.GetComponent<MainCharacterComponent>().RebirthPointId_SetFromGame(RebirthPointID);
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
        var owner = __instance.GetOwner();
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEntityByCharacter(character, out var entity))
        {
            entity.Value.GetComponent<MainCharacterComponent>().RebirthPointId_SetFromGame(BirthPointID);
        }
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
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEntityByCharacter(character, out var entity))
        {
            entity.Value.GetComponent<MainCharacterComponent>().RebirthPointId_SetFromGame(RebirthPointId);
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnRebirthFinished
{
    [HarmonyTargetMethodHint("b1.BUS_RebirthComp", "CommonRebirthLogic")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_RebirthComp:CommonRebirthLogic");
    }

    public static void Postfix(UActorCompBaseCS __instance)
    {
        var owner = __instance.GetOwner();
        var entity = DI.Instance.PawnState.GetEntityByPlayerActor(owner);

        if (entity.HasValue)
        {
            entity.Value.GetLocalState().IsRespawning = false;
            DI.Instance.MappedEvent.PropagateToEcs(new AfterRebirthEvent(entity.Value));
        }
    }
}

[HarmonyPatch(typeof(UBGUFunctionLibCollisionChannel), nameof(UBGUFunctionLibCollisionChannel.BGUSetCollisionResponseToChannels))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchBGUSetCollisionResponseToChannels
{
    public static bool Prefix(UPrimitiveComponent Comp, Dictionary<ECollisionChannel, ECollisionResponseType> ResponseToChannels)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = Comp.GetOwner();

        if (owner == null)
            return true;

        // do not set to Custom if the owner is a synchronized player
        var player = DI.Instance.PawnState.GetEntityByPlayerActor(owner);

        if (!player.HasValue)
            return true;

        DI.Instance.Logger.LogDebug("Prevented BGUSetCollisionResponseToChannels for player {Pawn}", player.Value.GetState().PlayerId);
        return player.Value == DI.Instance.PlayerState.LocalMainCharacter;
    }
}

[HarmonyPatch(typeof(FUStSkillSDesc), "get_CooldownTime")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchSkillCooldownTime
{
    public static void Postfix(ref float __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (DI.Instance.AreaState.CurrentArea.HasValue && DI.Instance.AreaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            __result *= (DI.Instance.PlayerState.LocalMainCharacter?.GetLocalState().InstantSkillCooldown ?? false) ? 0f : 1f;
        }
    }
}