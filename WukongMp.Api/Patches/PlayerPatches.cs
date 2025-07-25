using System.Collections.Generic;
using b1;
using B1UI.GSUI;
using BtlB1;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using System.Reflection;
using System.Threading.Tasks;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.State;
using WukongMp.Api.WukongUtils;

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
            if (!DI.Instance.RelayClient.InRoom)
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

            var players = DI.Instance.Players;

            if (Owner == players.LocalPlayerState.Pawn)
            {
                var localState = players.LocalPlayerState;

                if (localState.IsStandRotate != __instance.IsStandRotate)
                {
                    players.LocalPlayerState.IsStandRotate = __instance.IsStandRotate;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.IsStandRotate), players.LocalPlayerState.IsStandRotate);
                }

                if (localState.IsAttacking != __instance.IsAttacking)
                {
                    players.LocalPlayerState.IsAttacking = __instance.IsAttacking;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.IsAttacking), players.LocalPlayerState.IsAttacking);
                }

                if (!players.LocalPlayerState.TurnInplaceTargetRotation.Equals(__instance.TurnInplaceTargetRotation, Constants.FloatComparisonTolerance))
                {
                    players.LocalPlayerState.TurnInplaceTargetRotation = __instance.TurnInplaceTargetRotation;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.TurnInplaceTargetRotation), players.LocalPlayerState.TurnInplaceTargetRotation);
                }

                if (!localState.TurnInplaceRemainAngle.Equals(__instance.TurnInplaceRemainAngle, Constants.FloatComparisonTolerance))
                {
                    players.LocalPlayerState.TurnInplaceRemainAngle = __instance.TurnInplaceRemainAngle;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.TurnInplaceRemainAngle), players.LocalPlayerState.TurnInplaceRemainAngle);
                }

                if (localState.OrientRotationToMovement != __instance.bOrientRotationToMovement)
                {
                    players.LocalPlayerState.OrientRotationToMovement = __instance.bOrientRotationToMovement;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.OrientRotationToMovement), players.LocalPlayerState.OrientRotationToMovement);
                }
            }
            else
            {
                var playerState = players.GetPlayerByActor(Owner);

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
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (Owner is not BGUCharacterCS)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var players = DI.Instance.Players;

            if (Owner == players.LocalPlayerState.Pawn)
            {
                var localState = players.LocalPlayerState;

                if (localState.ShouldWaitRotateFinished != __instance.bShouldWaitRotateFinished)
                {
                    players.LocalPlayerState.ShouldWaitRotateFinished = __instance.bShouldWaitRotateFinished;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.ShouldWaitRotateFinished), players.LocalPlayerState.ShouldWaitRotateFinished);
                }
            }
            else
            {
                var playerState = players.GetPlayerByActor(Owner);

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
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (Owner is not BGUCharacterCS)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var players = DI.Instance.Players;

            if (Owner == players.LocalPlayerState.Pawn)
            {
                var localState = players.LocalPlayerState;

                if (localState.InJump != __instance.bInJump)
                {
                    players.LocalPlayerState.InJump = __instance.bInJump;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.InJump), players.LocalPlayerState.InJump);
                }
            }
            else
            {
                var playerState = players.GetPlayerByActor(Owner);

                if (playerState == null)
                {
                    return;
                }

                __instance.bInJump = playerState.InJump;
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
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (Owner is not BGUCharacterCS character)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var players = DI.Instance.Players;

            if (Owner == players.LocalPlayerState.Pawn)
            {
                var localState = players.LocalPlayerState;

                if (localState.MoveSpeedLevel != __instance.MoveSpeedLevel)
                {
                    players.LocalPlayerState.MoveSpeedLevel = __instance.MoveSpeedLevel;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.MoveSpeedLevel), players.LocalPlayerState.MoveSpeedLevel);
                }

                if (localState.MoveSpeedState != __instance.MoveSpeedState)
                {
                    players.LocalPlayerState.MoveSpeedState = __instance.MoveSpeedState;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.MoveSpeedState), players.LocalPlayerState.MoveSpeedState);
                }
            }
            else
            {
                var playerState = players.GetPlayerByActor(Owner);

                if (playerState != null)
                {
                    __instance.MoveSpeedLevel = playerState.MoveSpeedLevel;
                    __instance.MoveSpeedState = playerState.MoveSpeedState;
                }
                else
                {
                    var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);

                    if (!entity.HasValue)
                        return; // unsynced entity

                    ref var anim = ref entity.Value.GetComponent<AnimationComponent>();

                    if (DI.Instance.RelayClient.IsMasterClient)
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
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            var players = DI.Instance.Players;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return false;
            }

            if (owner == players.LocalPlayerState.Pawn)
            {
                CacheEquipmentChange(EquipPosition, EquipID);
            }

            return owner == GameUtils.GetControlledPawn() || owner.GetName().Contains("Preview") || owner.GetName().Contains("Performer"); // TODO: Exact comparison
        }
        
        public static void CacheEquipmentChange(EquipPosition position, int newEq)
        {
            DI.Instance.Players.LocalPlayerState.Equipment.SetEquipment(position, newEq);
            DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.Equipment), DI.Instance.Players.LocalPlayerState.Equipment);
        }

    }

    [HarmonyPatch(typeof(BUS_DeadComp), "OnUnitDead")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitDead
    {
        private static int _pendingDaSheng;
        private static readonly HashSet<NetworkIdComponent> SpawnedDaSheng2 = [];

        public static void Prefix(BUS_DeadComp __instance, EDeadReason DeadReason, AActor Attacker, IBUC_SimpleStateData ___SimpleStateData, IBUC_UnitStateData ___UnitStateData, out bool __state)
        {
            __state = false;

            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DeadReason == EDeadReason.PlayerTrans)
                return; // TODO: Camera is broken after transformation, stuck in one direction

            var players = DI.Instance.Players;
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

            if (DI.Instance is { RelayClient.IsMasterClient: true, RoomState.InPvP: true, RoomState.InCombatRound: true })
            {
                if (Attacker != owner)
                {
                    var attackerPlayerState = players.GetPlayerByActor(Attacker);
                    var killedPlayerState = players.GetPlayerByActor(owner);
                    if (attackerPlayerState != null && killedPlayerState != null)
                    {
                        DI.Instance.Chatter.SendServerMessage("PlayerKilledPlayer", attackerPlayerState.NickName, killedPlayerState.NickName);
                    }
                }

                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);
                if (entity.HasValue)
                {
                    var tamerClass = entity.Value.GetComponent<LocalTamerComponent>().Tamer?.GetClass();
                    var netId = entity.Value.GetComponent<NetworkIdComponent>();
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

            var players = DI.Instance.Players;
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

            if (owner == players.LocalPlayerState.Pawn)
            {
                FreeCameraManager.Instance.EnterFreeCameraMode();
                return;
            }

            var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);
            if (entity.HasValue)
            {
                if (entity.Value.HasComponent<LocalTamerComponent>())
                {
                    ref var localTamerComp = ref entity.Value.GetComponent<LocalTamerComponent>();
                    localTamerComp.IsMonsterSynced = false;
                    localTamerComp.IsLocallySpawned = false;
                }

                if (entity.Value.HasComponent<TamerComponent>())
                {
                    ref var tamerComp = ref entity.Value.GetComponent<TamerComponent>();
                    TamerUtils.ClearSpawnedUnit(entity.Value);
                }

                if (!DI.Instance.RelayClient.IsMasterClient)
                    return;

                if (entity.Value.TryGetComponent<NetworkIdComponent>(out var networkId))
                {
                    // TODO: send attacker and anim montage
                    var payload = new UnitDeadPacket(networkId, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
                    DI.Instance.Rpc.SendUnitDead(payload);
                    Logging.LogDebug("Entity {Entity} died, sending UnitDead event", networkId);
                }
                else
                {
                    Logging.LogError("Entity {Entity} does not have NetworkIdComponent, skipping entity deletion", entity.Value.ToString());
                }
            }
        }
    }

    [HarmonyPatch(typeof(UIDeath), "DoShowIn")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchUIDeath
    {
        public static bool Prefix()
        {
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            var players = DI.Instance.Players;

            var localPawn = players.LocalPlayerState.Pawn;
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
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            var players = DI.Instance.Players;

            var owner = __instance.GetOwner();
            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return false;
            }

            if (___TargetInfoData.GetTargetInfo()?.LockTargetActor == NewTargetInfo.LockTargetActor)
                return true;

            NetworkIdComponent newTargetId = default;
            var clearTarget = true;
            string name = string.Empty;

            var newTargetPlayerState = players.GetPlayerByActor(NewTargetInfo?.LockTargetActor);
            var newTargetMonsterState = DI.Instance.PawnRegistry.GetMonsterByActor(NewTargetInfo?.LockTargetActor);

            if (NewTargetInfo != null && NewTargetInfo.LockTargetActor != null && newTargetPlayerState == null && !newTargetMonsterState.HasValue)
            {
                // not synchronized character targeted
                return true;
            }

            if (newTargetPlayerState != null)
            {
                newTargetId = NetworkIdComponent.FromPlayerId(newTargetPlayerState.PlayerId);
                name = newTargetPlayerState.NickName;
                clearTarget = false;
            }
            else if (newTargetMonsterState.HasValue)
            {
                newTargetId = newTargetMonsterState.Value.GetComponent<NetworkIdComponent>();
                name = newTargetMonsterState.Value.GetComponent<NicknameComponent>().Nickname;
                clearTarget = false;
            }

            // send only own updates
            if (owner == players.LocalPlayerState.Pawn)
            {
                Logging.LogDebug("New target sent for {Subject} as: {Target}", players.LocalPlayerState.NickName, name);
                DI.Instance.Rpc.SendSetTarget(new TargetData(NetworkIdComponent.FromPlayerId(players.LocalPlayerState.PlayerId), newTargetId, clearTarget));
                return true;
            }

            // master sends targets for monsters
            if (!DI.Instance.RelayClient.IsMasterClient)
                return false;

            var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);
            if (entity.HasValue)
            {
                Logging.LogDebug("New target sent for monster: {Subject} as: {Target}", players.LocalPlayerState.NickName, name);

                var netId = entity.Value.GetComponent<NetworkIdComponent>();
                DI.Instance.Rpc.SendSetTarget(new TargetData(netId, newTargetId, clearTarget));
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
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            if (!Constants.IsCoop)
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
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DI.Instance.RelayClient.IsMasterClient)
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
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            __result = DI.Instance.RoomState.EnemiesNgPlusLevel + 1;
            return false;
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnTriggerInputActionImpl")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchOnTriggerInputActionImpl
    {
        public static bool Prefix(BUS_PlayerInputActionComp __instance)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            var players = DI.Instance.Players;
            return !(players.LocalPlayerState.Pawn == __instance.GetOwner() && players.LocalPlayerState.IsSpectator);
        }
    }

    // Disable slowing down time
    [HarmonyPatch(typeof(BUS_TimeScaleComp), "OnTriggerScaleTime")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchOnTriggerScaleTime
    {
        public static bool Prefix()
        {
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
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
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (__instance.GetOwner() == GameUtils.GetControlledPawn() && ___MovementData.GetMoveType() == EBGUMoveMode.AIPathMove)
            {
                var localPlayerState = DI.Instance.Players.LocalPlayerState;
                if (___ChrData.RealWorldVelocity.IsNearlyZero())
                {
                    Logging.LogDebug("RealWorldVelocity is nearly zero");
                    localPlayerState.AIPathMoveStuckTimer += DeltaTime;
                    if (localPlayerState.AIPathMoveStuckTimer > Constants.AIPathMoveStuckTimeout)
                    {
                        Logging.LogDebug("AIPathMove stuck detected, resetting timer");
                        localPlayerState.AIPathMoveStuckTimer = 0f;
                        localPlayerState.IsAIPathMoveStuck = true;
                        var events = BUS_EventCollectionCS.Get(__instance.GetOwner());
                        events.Evt_MovementForceStop.Invoke();
                    }
                }
                else
                {
                    localPlayerState.AIPathMoveStuckTimer = 0f;
                    localPlayerState.IsAIPathMoveStuck = false;
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
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            var localPlayerState = DI.Instance.Players.LocalPlayerState;
            if (localPlayerState.IsAIPathMoveStuck)
            {
                localPlayerState.IsAIPathMoveStuck = false;
                __instance.StepFinish();
                return false;
            }

            return true;
        }
    }
}