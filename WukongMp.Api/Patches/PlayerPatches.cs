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
using WukongMp.Api.Old;
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

            var client = WukongMpMod.Client;

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

                if (!client.LocalPlayerState.TurnInplaceTargetRotation.Equals(__instance.TurnInplaceTargetRotation, Constants.FloatComparisonTolerance))
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

            var client = WukongMpMod.Client;

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

            var client = WukongMpMod.Client;

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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (Owner is not BGUCharacterCS character)
                return;

            if (Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var client = WukongMpMod.Client;

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
                    var entity = WukongMpMod.Instance.GetMonsterByActor(character);

                    if (!entity.HasValue)
                        return; // unsynced entity

                    ref var anim = ref entity.Value.GetComponent<AnimationComponent>();

                    if (client.IsMasterClient)
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMpMod.Client;
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

            return owner == GameUtils.GetControlledPawn() || owner.GetName().Contains("Preview") || owner.GetName().Contains("Performer"); // TODO: Exact comparison
        }
    }

    [HarmonyPatch(typeof(BUS_DeadComp), "OnUnitDead")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitDead
    {
        public static void Prefix(BUS_DeadComp __instance, EDeadReason DeadReason, AActor Attacker, IBUC_SimpleStateData ___SimpleStateData, IBUC_UnitStateData ___UnitStateData, out bool __state)
        {
            __state = false;

            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (DeadReason == EDeadReason.PlayerTrans)
                return; // TODO: Camera is broken after transformation, stuck in one direction

            var client = WukongMpMod.Client;
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

            if (client is { IsMasterClient: true, RoomState.InPvP: true, RoomState.InCombatRound: true })
            {
                if (Attacker != owner)
                {
                    var attackerPlayerState = client.GetPlayerByActor(Attacker);
                    var killedPlayerState = client.GetPlayerByActor(owner);
                    if (attackerPlayerState != null && killedPlayerState != null)
                    {
                        client.WukongChat.SendServerMessage("PlayerKilledPlayer", attackerPlayerState.NickName, killedPlayerState.NickName);
                    }
                }

                var entity = WukongMpMod.Instance.GetMonsterByActor(owner);
                if (entity.HasValue)
                {
                    var tamerClass = entity.Value.GetComponent<LocalTamerComponent>().Tamer?.GetClass();
                    if (tamerClass != null && tamerClass.PathName == UnitPathsConfig.GetUnitPath(CharacterKind.DaSheng))
                    {
                        var teamId = ownerCharacter.GetTeamIDInCS();
                        var location = ownerCharacter.GetActorLocation();

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(5000);
                            Utils.TryRunOnGameThread(() => { SpawningUtils.SpawnUnitMaster(CharacterKind.DaSheng2, location, teamId); });
                        });
                        return;
                    }
                }

                client.CheckRoundEndCondition();
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

            var client = WukongMpMod.Client;
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

            if (owner == client.LocalPlayerState.Pawn)
            {
                FreeCameraManager.Instance.EnterFreeCameraMode();
                return;
            }

            var entity = WukongMpMod.Instance.GetMonsterByActor(owner);
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

                if (!client.IsMasterClient)
                    return;

                if (entity.Value.TryGetComponent<NetworkIdComponent>(out var networkId))
                {
                    // TODO: send attacker and anim montage
                    var payload = new UnitDeadPacket(networkId, DeadReason, DmgID, StiffLevel, bIsDotDmg, AbnormalType);
                    WukongMpMod.Instance.SendUnitDead(payload);
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

            var client = WukongMpMod.Client;

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

        public static bool Prefix(UnitLockTargetInfo NewTargetInfo, BUC_TargetInfoData ___TargetInfoData, UActorCompBaseCS __instance)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMpMod.Client;

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

            var newTargetPlayerState = client.GetPlayerByActor(NewTargetInfo?.LockTargetActor);
            var newTargetMonsterState = WukongMpMod.Instance.GetMonsterByActor(NewTargetInfo?.LockTargetActor);

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
            if (owner == client.LocalPlayerState.Pawn)
            {
                Logging.LogDebug("New target sent for {Subject} as: {Target}", client.LocalPlayerState.NickName, name);
                WukongMpMod.Instance.SendSetTarget(new TargetData(NetworkIdComponent.FromPlayerId(client.LocalPlayerState.PlayerId), newTargetId, clearTarget));
                return true;
            }

            // master sends targets for monsters
            if (!client.IsMasterClient)
                return false;

            var entity = WukongMpMod.Instance.GetMonsterByActor(owner);
            if (entity.HasValue)
            {
                Logging.LogDebug("New target sent for monster: {Subject} as: {Target}", client.LocalPlayerState.NickName, name);

                var netId = entity.Value.GetComponent<NetworkIdComponent>();
                WukongMpMod.Instance.SendSetTarget(new TargetData(netId, newTargetId, clearTarget));
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
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
        public static void Postfix(BUS_BeAttackedComp __instance, AActor Attacker)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMpMod.Client;
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

            var client = WukongMpMod.Client;

            __result = client.RoomState.EnemiesNgPlusLevel + 1;
            return false;
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnTriggerInputActionImpl")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchOnTriggerInputActionImpl
    {
        public static bool Prefix(BUS_PlayerInputActionComp __instance)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMpMod.Client;
            return !(client.LocalPlayerState.Pawn == __instance.GetOwner() && client.LocalPlayerState.IsSpectator);
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var guid = BGU_DataUtil.GetActorGuid(__instance.GetOwner());
            Logging.LogWarning("BUS_QuestDynamicObstacleComp.EnableCollision called for {Guid}", guid);

            return !DisabledCollidersData.IsDisabled(guid);
        }
    }

    [HarmonyPatch(typeof(BUS_PlayerMovementSystem), "TickInputMoving")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchTickInputMoving
    {
        public static void Postfix(float DeltaTime, BUS_PlayerMovementSystem __instance, BUC_MovementData ___MovementData, IBUC_ABPCharacterData ___ChrData)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (__instance.GetOwner() == GameUtils.GetControlledPawn() && ___MovementData.GetMoveType() == EBGUMoveMode.AIPathMove)
            {
                var localPlayerState = WukongMpModBase.Client.LocalPlayerState;
                if (___ChrData.RealWorldVelocity.IsNearlyZero())
                {
                    Logging.LogWarning("RealWorldVelocity is nearly zero");
                    localPlayerState.AIPathMoveStuckTimer += DeltaTime;
                    if (localPlayerState.AIPathMoveStuckTimer > Constants.AIPathMoveStuckTimeout)
                    {
                        Logging.LogWarning("AIPathMove stuck detected, resetting timer");
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var localPlayerState = WukongMpModBase.Client.LocalPlayerState;
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