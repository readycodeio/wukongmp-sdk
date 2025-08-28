using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using b1;
using B1UI.GSUI;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using ReadyM.Api.Multiplayer.ECS.Values;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using EquipPosition = BtlB1.EquipPosition;

namespace WukongMp.Api.Patches
{
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
                mainEntity = pawnState.GetByEntityByPlayerPawn(Owner);
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
                var mainEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(Owner);
                if (!mainEntity.HasValue)
                    return;

                ref var mainComp = ref mainEntity.Value.GetState();
                __instance.bShouldWaitRotateFinished = mainComp.ShouldWaitRotateFinished;
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

                if (mainComp.InJump != __instance.bInJump)
                {
                    mainComp.InJump = __instance.bInJump;
                }
            }
            else
            {
                var mainEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(Owner);
                if (!mainEntity.HasValue)
                    return;

                ref var mainComp = ref mainEntity.Value.GetState();
                __instance.bInJump = mainComp.InJump;
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
            AActor Owner,
            IBUC_ABPCharacterData ChrData,
            IBUC_ABPBGUCharacterData BGUData,
            IBUC_SpeedCtrlData SpeedCtrlData,
            float DeltaTime)
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
                var mainEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(Owner);

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

            return owner == GameUtils.GetControlledPawn() || owner.GetName().Contains("Preview") || owner.GetName().Contains("Performer"); // TODO: Exact comparison
        }
    }

    [HarmonyPatch(typeof(BUS_DeadComp), "OnUnitDead")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitDead
    {
        private static int _pendingDaSheng;
        private static readonly HashSet<NetworkId> SpawnedDaSheng2 = [];

        public static void Prefix(BUS_DeadComp __instance, EDeadReason DeadReason, AActor Attacker, IBUC_SimpleStateData ___SimpleStateData, IBUC_UnitStateData ___UnitStateData, out bool __state)
        {
            __state = false;

            if (!DI.Instance.AreaState.InRoom)
                return;

            if (DeadReason == EDeadReason.PlayerTrans)
                return; // TODO: Camera is broken after transformation, stuck in one direction

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

            if (DI.Instance?.AreaState is
                { IsMasterClient: true, CurrentArea.Room: { InPvP: true, InCombatRound: true } })
            {
                if (Attacker != owner)
                {
                    var attackerMainEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(Attacker);
                    var killedMainEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(owner);

                    if (attackerMainEntity != null && killedMainEntity != null)
                    {
                        ref var attackerMain = ref attackerMainEntity.Value.GetState();
                        ref var killedMain = ref killedMainEntity.Value.GetState();

                        // FIXME: This is not the place to do this. Invert control: it's the chatter that should subscribe to
                        // game events and that should report messages
                        DI.Instance.Chatter.SendServerMessage("PlayerKilledPlayer", attackerMain.CharacterNickName, killedMain.CharacterNickName);
                    }
                }

                var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
                if (tamerEntity.HasValue)
                {
                    ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
                    var tamerClass = localTamer.Tamer?.GetClass();
                    var netId = tamerEntity.Value.GetMeta().NetId;
                    if (tamerClass != null && tamerClass.PathName == UnitPathsConfig.GetUnitPath(CharacterKind.DaSheng))
                    {
                        var teamId = ownerCharacter.GetTeamIDInCS();
                        var location = ownerCharacter.GetActorLocation();

                        if (SpawnedDaSheng2.Add(netId))
                        {
                            _pendingDaSheng++;
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(5000);
                                Utils.TryRunOnGameThread(() =>
                                {
                                    SpawningUtils.SpawnUnitMaster(CharacterKind.DaSheng2, location, teamId);
                                    _pendingDaSheng--;
                                });
                            });
                        }
                        else
                        {
                            Logging.LogDebug("Would spawn DaSheng2, but already spawned for this monster: {Monster}", netId);
                        }

                        return;
                    }
                }

                if (_pendingDaSheng == 0)
                {
                    DI.Instance.PVP?.CheckRoundEndCondition();
                }
            }
        }

        public static void Postfix(
            BUS_DeadComp __instance,
            bool __state,
            IBUC_SimpleStateData ___SimpleStateData,
            IBUC_UnitStateData ___UnitStateData,
            EDeadReason DeadReason,
            AActor Attacker,
            int DmgID = -1,
            int StiffLevel = -1,
            UAnimMontage? BeAttackedAM = null,
            bool bIsDotDmg = false,
            EAbnormalStateType AbnormalType = EAbnormalStateType.None)
        {
            if (!__state)
                return; // skipped prefix

            var playerState = DI.Instance.PlayerState;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (owner is not BGUCharacterCS)
            {
                return;
            }

            if (owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
            {
                FreeCameraManager.Instance.EnterFreeCameraMode();
                return;
            }

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue)
            {
                ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
                localTamer.IsMonsterSynced = false;
                localTamer.IsLocallySpawned = false;

                TamerUtils.ClearSpawnedUnitRefCount(tamerEntity.Value);

                if (!DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                    return;

                ref var meta = ref tamerEntity.Value.GetMeta();

                // TODO: send attacker and anim montage
                var payload = new UnitDeadPacket(meta.NetId, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
                DI.Instance.Rpc.SendUnitDead(payload);
                Logging.LogDebug("Entity {Entity} died, sending UnitDead event", meta.NetId);
            }
        }
    }

    [HarmonyPatch(typeof(UIDeath), "DoShowIn")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchUIDeath
    {
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
            string name = string.Empty;

            var newTargetPlayerEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(NewTargetInfo?.LockTargetActor);
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
                name = newTargetMonsterEntity.Value.GetNickname().Nickname;
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
                Logging.LogDebug("New target sent for monster: {Subject} as: {Target}", tamerEntity.Value.GetNickname().Nickname, name);

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

            if (Constants.IsPvP)
            {
                InControlData.ArmLength = Constants.CameraArmLength;
                InControlData.ArmTargetOffset = FVector.ZeroVector;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BUS_BeAttackedComp), "DoDamageLogic")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchDoDamageLogic
    {
        public static void Postfix(BUS_BeAttackedComp __instance, AActor? Attacker)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            if (DI.Instance.AreaState.IsMasterClient)
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

            return !(mainEntity?.GetLocalState().Pawn == __instance.GetOwner() && playerEntity?.GetState().IsSpectator == true);
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

            var guid = BGU_DataUtil.GetActorGuid(__instance.GetOwner());

            return !DisabledCollidersData.IsDisabled(guid);
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerMovementSystem), "TickInputMoving")]
    [HarmonyPatchCategory(Constants.DisabledPatches)]
    public class PatchTickInputMoving
    {
        public static void Postfix(float DeltaTime, BUS_PlayerMovementSystem __instance, BUC_MovementData ___MovementData, IBUC_ABPCharacterData ___ChrData)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            if (__instance.GetOwner() == GameUtils.GetControlledPawn() && ___MovementData.GetMoveType() == EBGUMoveMode.AIPathMove)
            {
                var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
                if (!mainEntity.HasValue)
                    return;
                ref var localMain = ref mainEntity.Value.GetLocalState();
                if (___ChrData.RealWorldVelocity.IsNearlyZero())
                {
                    Logging.LogDebug("RealWorldVelocity is nearly zero");
                    localMain.AIPathMoveStuckTimer += DeltaTime;
                    if (localMain.AIPathMoveStuckTimer > Constants.AiPathMoveStuckTimeout)
                    {
                        Logging.LogDebug("AIPathMove stuck detected, resetting timer");
                        localMain.AIPathMoveStuckTimer = 0f;
                        localMain.IsAIPathMoveStuck = true;
                        var events = BUS_EventCollectionCS.Get(__instance.GetOwner());
                        events.Evt_MovementForceStop.Invoke();
                    }
                }
                else
                {
                    localMain.AIPathMoveStuckTimer = 0f;
                    localMain.IsAIPathMoveStuck = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(InteractStepMatchPos), "OnInteractMatchingPosFinish")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnInteractMatchingPosFinish
    {
        public static bool Prefix(InteractStepMatchPos __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var playerState = DI.Instance.PlayerState;
            var mainEntity = playerState.LocalMainCharacter;
            if (!mainEntity.HasValue)
                return true;

            ref var localMain = ref mainEntity.Value.GetLocalState();
            if (localMain.IsAIPathMoveStuck)
            {
                localMain.IsAIPathMoveStuck = false;
                __instance.StepFinish();
                return false;
            }

            return true;
        }
    }
}