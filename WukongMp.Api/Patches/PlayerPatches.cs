using System;
using System.Collections.Generic;
using System.Reflection;
using b1;
using B1UI.GSSvc;
using BtlB1;
using BtlShare;
using Friflo.Engine.ECS;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using PreludeLib.Attributes;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

// runs multithreaded
[HarmonyPatch(typeof(BUC_ABPBGUCharacterData), nameof(BUC_ABPBGUCharacterData.Update_GameThread))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBGUPlayerAnimation
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchPlayerLocomotion
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBasicData
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchEqCompUpdate
{
    public static bool Prefix(BUS_EquipComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (owner.GetName().Contains("Preview") // preview actor in EQ view
            || owner.GetName().Contains("Performer") // cutscene actor?
            || owner.GetName().Contains("monkeysummon")) // summoned clones
            return true;

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
        {
            return DI.Instance.MappingPolicyDir.ForData<MainCharacterComponent>().CanGameSetLocally(entity.Value);
        }

        return false;
    }

    public static void Postfix(BUS_EquipComp __instance, EquipPosition EquipPosition)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is destroyed");
            return;
        }

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
        {
            if (DI.Instance.MappedField.CanLoadFromGame<MainCharacterComponent>(entity.Value, out var loader))
            {
                loader.LoadFromGame(MainCharacterComponent.Fields.Equipment.In<(BGUCharacterCS, ReadyM.Wukong.Common.ECS.Values.EquipPosition)>(), ((BGUCharacterCS)owner, EquipPosition.FromGame()));
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_DeadComp), "OnUnitDead")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnUnitDead
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

        if (owner is not BGUCharacterCS || ___UnitStateData.HasState(EBGUUnitState.Dead) || ___SimpleStateData.HasSimpleState(EBGUSimpleState.PendingDeathInAnimationSyncing))
        {
            return;
        }

        __state = true;
    }

    public static void Postfix(
        BUS_DeadComp __instance,
        bool __state,
        EDeadReason DeadReason,
        AActor? Attacker,
        int DmgID = -1,
        int StiffLevel = -1,
        bool bIsDotDmg = false,
        EAbnormalStateType AbnormalType = EAbnormalStateType.None)
    {
        if (!__state)
            return; // skipped prefix

        if (DeadReason == EDeadReason.PlayerTrans)
            return;

        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        if (owner is not BGUCharacterCS ownerCharacter)
            return;

        Entity? attackerEntity = null;
        if (Attacker is BGUCharacterCS attackerCharacter)
        {
            DI.Instance.PawnState.TryGetEntityByCharacter(attackerCharacter, out attackerEntity);
        }

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(ownerCharacter, out var entity) &&
            DI.Instance.MappingPolicyDir.ForEvent<UnitDeadEvent>().CanGameEventNotifyEcs(entity.Value))
        {
            var state = entity.Value.GetState();
            if (!state.IsTransformed)
            {
                ref var localState = ref entity.Value.GetLocalState();
                localState.IsDuringDeathAnim = true;
                var battleData = BGU_DataUtil.GetReadOnlyData<IBPC_BattleMainInfoData, BPC_BattleMainInfoData>(entity.Value.Pawn?.GetController());
                if (battleData != null)
                {
                    localState.DeadAnimationTime = battleData.PlayerDeathUIDelayTime;
                }

                localState.DeadAnimationTime = 6f; // Value from game.

                if (DI.Instance.MappedField.CanLoadFromGame<HpComponent>(entity.Value, out var load))
                {
                    load.SetFromGame(HpComponent.Fields.IsDead, true);
                }

                // TODO: Check required before call to this
                DI.Instance.MappedEvent.NotifyEcsIfApplicable(new UnitDeadEvent(entity.Value, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType), entity.Value.Entity);
                DI.Instance.GameplayEventRouter.RaiseOnUnitDead(entity.Value, attackerEntity);
                Logging.LogDebug("Player {PlayerId} died, sending UnitDead event", state.PlayerId);
            }

            return;
        }

        if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(ownerCharacter, out var tamerEntity))
        {
            if (DI.Instance.MappedField.CanLoadFromGame<HpComponent>(tamerEntity.Value, out var load))
            {
                load.SetFromGame(HpComponent.Fields.IsDead, true);
            }
            var payload = new UnitDeadEvent(tamerEntity.Value, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(payload, tamerEntity.Value.Entity);
            DI.Instance.GameplayEventRouter.RaiseOnUnitDead(tamerEntity.Value, attackerEntity);
            Logging.LogDebug("Entity {Entity} died, sending UnitDead event", tamerEntity.Value.GetNetId());
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnUnitTriggerDead
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchCameraCompTick
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchFallDamage
{
    public static bool Prefix()
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        return false;
    }
}

[HarmonyPatch(typeof(BUC_TargetInfoData), "IsSupportMultiLockTarget")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchIsSupportMultiLockTarget
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchSetTargetToData
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
        var name = "null (Clear target)";

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
            name = newTargetPlayerEntity.Value.GetNickname().Nickname;
            clearTarget = false;
        }
        else if (newTargetMonsterEntity.HasValue)
        {
            newTarget = newTargetMonsterEntity.Value;
            name = newTargetMonsterEntity.Value.GetTamer().Guid ?? "Unknown monster";
            clearTarget = false;
        }

        if (DI.Instance.MappingPolicyDir.IsCharacterMapped(owner, out var entity))
        {
            Logging.LogDebug("New target sent for {Subject} as: {Target}", owner.GetName(), name);
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new SetTargetEvent(entity.Value, newTarget, clearTarget), entity.Value);
            return true;
        }

        return false;
    }
}

[HarmonyPatch(typeof(BUS_PlayerCameraCompImpl), "ApplyCameraControlData")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchApplyCameraControlData
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchIsDamageValid
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchCheckStrideDown
{
    public static bool Prefix()
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        return false;
    }
}

[HarmonyPatch(typeof(BGW_GameDB), "GetUnitBattleInfoExtendDesc")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchGetUnitBattleInfoExtendDesc
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnTriggerInputActionImpl
{
    public static bool Prefix(BUS_PlayerInputActionComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var playerState = DI.Instance.PlayerState;

        var mainEntity = playerState.LocalMainCharacter;
        if (!mainEntity.HasValue)
            return true;

        return !(mainEntity.Value.Pawn == __instance.GetOwner() && mainEntity.Value.GetState().IsSpectator);
    }
}

// Disable slowing down time
[HarmonyPatch(typeof(BUS_TimeScaleComp), "OnTriggerScaleTime")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnTriggerScaleTime
{
    public static bool Prefix()
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        return false;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchSetAllUnitCannotDead
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

[HarmonyPatch(typeof(InteractStepMatchPos), "StepBegin")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnInteractStepBegin
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTriggerFinish
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnUIShrineMainActive
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchInteractStepMatchPosOnTick
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnRebirthPointRest
{
    public static bool Prefix()
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var playerController = GameUtils.GetPlayerController()!;
        var owner = playerController.GetControlledPawn();

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var mainEntity))
        {
            var rebirthPointData = BGU_DataUtil.GetReadOnlyData<BPC_RebirthPointData>(GameUtils.GetPlayerController());
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new RestAtShrineEvent(mainEntity.Value, rebirthPointData.CurrentBirthPoint.PointID), mainEntity.Value.Entity);
        }

        return true;
    }
}

[HarmonyPatch(typeof(BUC_ABPMotionMatchingData), "UpdatePlayerMotionMatchingState")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchUpdatePlayerMotionMatchingState
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

[HarmonyPatch(typeof(BUS_JumpComp), "TriggerJumpSkill", typeof(ESkillDirection), typeof(FVector2D))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTriggerJumpSkill
{
    public static void Prefix(BUS_JumpComp __instance, ESkillDirection StartJumpDir, FVector2D CurrentInputVector)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var mainEntity))
        {
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new StartJumpEvent(mainEntity.Value, StartJumpDir, CurrentInputVector), mainEntity.Value.Entity);
        }
    }
}

[HarmonyPatch(typeof(BUS_JumpComp), "OnReleased")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchJumpOnReleased
{
    public static void Prefix(BUS_JumpComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var mainEntity))
        {
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new StopJumpEvent(mainEntity.Value), mainEntity.Value.Entity);
        }
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "CheckCanSelectTarget")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchCheckCanSelectTarget
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchSetAttrTransAfterActiveTalent
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnSetRebirthPointAsCurrentBirthPoint
{
    public static void Postfix(UActorCompBaseCS __instance, int RebirthPointID)
    {
        PlayerUtils.LogRebirthPointChange(__instance.GetOwner(), RebirthPointID);
        var owner = __instance.GetOwner();
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEntityByCharacter(character, out var entity))
        {
            entity.Value.GetComponent<MainCharacterComponent>().RebirthPointId = RebirthPointID;
        }
    }
}

[HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnSetCurrentBirthPoint")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnSetCurrentBirthPoint
{
    public static void Postfix(UActorCompBaseCS __instance, int BirthPointID)
    {
        PlayerUtils.LogRebirthPointChange(__instance.GetOwner(), BirthPointID);
        var owner = __instance.GetOwner();
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEntityByCharacter(character, out var entity))
        {
            entity.Value.GetComponent<MainCharacterComponent>().RebirthPointId = BirthPointID;
        }
    }
}

[HarmonyPatch(typeof(BPS_RebirthPointSystem), "OnForceSetRebirthPoint")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnForceSetRebirthPoint
{
    public static void Postfix(UActorCompBaseCS __instance, int RebirthPointId)
    {
        PlayerUtils.LogRebirthPointChange(__instance.GetOwner(), RebirthPointId);
        var owner = __instance.GetOwner();
        if (owner is BGUCharacterCS character && DI.Instance.PawnState.TryGetEntityByCharacter(character, out var entity))
        {
            entity.Value.GetComponent<MainCharacterComponent>().RebirthPointId = RebirthPointId;
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnRebirthFinished
{
    [HarmonyTargetMethodHint("b1.BUS_RebirthComp", "CommonRebirthLogic")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_RebirthComp:CommonRebirthLogic");
    }

    public static void Postfix(UActorCompBaseCS __instance)
    {
        var owner = __instance.GetOwner();

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
        {
            entity.Value.GetLocalState().IsRespawning = false;
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new AfterRebirthEvent(entity.Value), entity.Value.Entity);
        }
    }
}

[HarmonyPatch(typeof(UBGUFunctionLibCollisionChannel), nameof(UBGUFunctionLibCollisionChannel.BGUSetCollisionResponseToChannels))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBGUSetCollisionResponseToChannels
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchSkillCooldownTime
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