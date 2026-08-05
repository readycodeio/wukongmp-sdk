using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using b1;
using Friflo.Engine.ECS;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Api.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGUFuncLibActorTransformCS), nameof(BGUFuncLibActorTransformCS.BGUSetActorLocation), typeof(AActor), typeof(FVector), typeof(bool), typeof(bool), typeof(FHitResult), typeof(bool), typeof(bool))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBGUSetActorLocationForPhysicsBasedMovement
{
    public static void Prefix(AActor NeedSetInfoActor, ref bool bTeleport)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (NeedSetInfoActor is BGU_CharacterAI ai && ai.GetActorGuid(out var guid) && guid == "UGuid.HYS.JiRuHuo01")
        {
            bTeleport = true;
        }
    }
}

[HarmonyPatch(typeof(BGUFuncLibActorTransformCS), nameof(BGUFuncLibActorTransformCS.BGUSetActorRotation))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBGUSetActorRotationForPhysicsBasedMovement
{
    public static void Prefix(AActor NeedSetInfoActor, ref bool bTeleportPhysics, ref bool bForceUpdate)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (NeedSetInfoActor is BGU_CharacterAI ai && ai.GetActorGuid(out var guid) && guid == "UGuid.HYS.JiRuHuo01")
        {
            bTeleportPhysics = true;
            bForceUpdate = true;
        }
    }
}

[HarmonyPatch(typeof(BGU_PhysicsSimulationMoveMode), "OnUpdate")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchPhysicsSimulationMoveMode
{
    public static void Postfix(ACharacter ___OwnerCharacter)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (___OwnerCharacter.IsNullOrDestroyed())
            return;

        var entity = DI.Instance.PawnState.GetEntityByTamerMonster(___OwnerCharacter);
        if (!entity.HasValue)
            return;

        ref var physComp = ref entity.Value.GetTransform();
        physComp.Rotation = ___OwnerCharacter.Mesh.GetSocketRotation(new FName("Head")).ToVector3();
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnRegisterTamer
{
    [HarmonyTargetMethodHint("b1.BGS_TamerManagerSystem", "OnRegisterTamer")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_TamerManagerSystem:OnRegisterTamer");
    }

    public static void Postfix(FTamerRef InTamer)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        Logging.LogDebug("Tamer {Tamer} registered by game", InTamer.TamerName);
    }
}

[HarmonyPatch(typeof(BUTamerActor), "BeginPlayCS_Implementation")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTamerBeginPlayCS_Implementation
{
    public static void Postfix(BUTamerActor __instance)
    {
        if (!DI.Instance.AreaState.InRoom || !DI.Instance.EventBus.IsGameplayLevel)
            return;

        if (DI.Instance.AreaState.IsMasterClient)
        {
            if (__instance.TamerType != ETamerType.Summoned && __instance.TamerType != ETamerType.Spawned)
            {
                var guid = BGU_DataUtil.GetActorGuid(__instance);
                var tamerEntity = DI.Instance.PawnState.GetEntityByTamerGuid(guid);
                if (tamerEntity == null)
                {
                    SpawningUtils.CreateMonsterInEcs(DI.Instance.PawnState, guid, __instance, Constants.DefaultMonsterTeamId, __instance.PathName);
                }
                else
                {
                    Logging.LogDebug("Monster already exists in ECS: {NetId}, guid: {Guid}", tamerEntity.Value.GetMeta().NetId, tamerEntity.Value.GetTamer().Guid);

                    // ensure Tamer is mapped and not pointing to a destroyed instance
                    if (tamerEntity.Value.Tamer.IsNullOrDestroyed())
                    {
                        tamerEntity.Value.SetTamer(__instance, true);
                        Logging.LogDebug("Updated tamer mapping for entity {Entity} to new instance (BeginPlayCS_Implementation)", tamerEntity.Value.Entity.Id);
                    }
                }
            }
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), "IncrementalBeginPlayUnit")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTamerLoad
{
    public static void Postfix(FTamerRef __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (!__instance.IsMonsterValid() || !__instance.InstancePtr.IsValid())
            return;

        var tamer = __instance.InstancePtr.Get();

        Logging.LogDebug("Monster {Guid} waking up locally", BGU_DataUtil.GetActorGuid(tamer));
        var monsterGuid = BGU_DataUtil.GetActorGuid(tamer.GetMonster());
        var tamerEntity = DI.Instance.PawnState.GetEntityByTamer(tamer);
        if (tamerEntity.HasValue)
        {
            TamerUtils.MarkMonsterLocallySpawned(DI.Instance.MappedEvent, tamerEntity.Value);
        }
        else if (!EcsExcludedMonsters.MonsterNames.Any(monsterGuid.Contains) && !DI.Instance.GameplayConfiguration.IsTamerNotSynchronized(monsterGuid))
        {
            Logging.LogError("Spawned monster is not in the ECS, guid: {Guid}", monsterGuid);
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.CanTurnBack2Loaded))]
[HarmonyPatchCategory(PatchCategory.Global)]
internal class PatchCanTurnBack2Loaded
{
    static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.TurnBack2Loaded))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTurnBack2Loaded
{
    static bool Prefix(FTamerRef __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (!__instance.IsMonsterValid() || !__instance.InstancePtr.IsValid())
            return true;

        var tamerActor = __instance.InstancePtr.Get();
        var tamerGuid = BGU_DataUtil.GetActorGuid(tamerActor);

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamer(tamerActor);
        if (tamerEntity.HasValue)
        {
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
            TamerUtils.MarkMonsterLocallyDespawned(DI.Instance.MappedEvent, tamerEntity.Value);
            localTamer.HasPendingUnload = true;

            ref var tamer = ref tamerEntity.Value.GetTamer();
            if (!tamer.ForceKeepSpawned)
            {
                Logging.LogDebug("Unloading monster {Guid} locally", tamerGuid);
                localTamer.IsMonsterActive = false;
                localTamer.HasPendingUnload = false;
                MarkerUtils.DestroyMarkerForCharacter(tamerEntity.Value);
                return true;
            }

            return false;
        }
        else if (!DI.Instance.GameplayConfiguration.IsTamerNotSynchronized(tamerGuid))
        {
            Logging.LogError("Unloading monster is not in the ECS, guid: {Guid}", tamerGuid);
        }

        return true;
    }
}

[HarmonyPatch(typeof(FTamerRef), "DestroyTamer")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTamerUnload
{
    public static void Prefix(FTamerRef __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        Logging.LogDebug("Tamer {Tamer} destroyed by game", __instance.TamerName);

        if (__instance.TamerType == ETamerType.Summoned || (__instance.TamerType == ETamerType.Spawned && DI.Instance.GameplayConfiguration.DeleteDestroyedTamersFromEcs))
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamer(__instance.InstancePtr.Value);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                tamerEntity.Value.SetTamer(null, false);
                Logging.LogDebug("Deleting tamer entity from ECS: id {Entity} (DestroyTamer)", tamerEntity.Value.GetMeta().NetId);
                DI.Instance.Scheduler.Scheduler.Schedule(static (cb, tid) => cb.DeleteEntity(tid), tamerEntity.Value.Entity.Id);
            }
        }
        else
        {
            // remove from any mappingcomponent pointing to it
            DI.Instance.World.Query<MappingComponent<AActor>>().ForEachEntity((ref mapping, entity) =>
            {
                if (mapping.GameObject is not null && mapping.GameObject.IsNullOrDestroyed())
                {
                    entity.Set(new MappingComponent<AActor>(null));
                    Logging.LogDebug("Removed destroyed actor from mapping component for entity {Entity}", entity.Id);
                }
            });
        }
    }
}

[HarmonyPatch(typeof(BUS_AIComp), "OnAIPerceptionSetting")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnAIPerceptionSetting
{
    public static bool Prefix(BUS_AIComp __instance, bool bEnable)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        if (owner != null)
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                return true;
        }

        return !bEnable;
    }
}

[HarmonyPatch(typeof(BUS_AIComp), "OnAIPauseBT")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnAIPauseBT
{
    public static bool Prefix(BUS_AIComp __instance, bool IsPause)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        if (owner != null)
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                return true;
        }

        return IsPause;
    }
}

[HarmonyPatch(typeof(BUS_AIComp), "OnEnableCanSetBT")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnEnableCanSetBT
{
    public static bool Prefix(BUS_AIComp __instance, bool bEnable)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        if (owner != null)
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                return true;
        }

        return !bEnable;
    }
}

[HarmonyPatch(typeof(BUS_FsmComp), "OnAIPauseFsm")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnAIPauseFsm
{
    public static bool Prefix(BUS_FsmComp __instance, bool IsPause)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        if (owner != null)
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                Logging.LogDebug("Setting FSM pause state to {IsPause} for tamer {Tamer}", IsPause, tamerEntity.Value.GetTamer().Guid);
                tamerEntity.Value.GetTamer().HasFsmPaused = IsPause;
                return true;
            }
        }

        return IsPause;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnEnableCanUpdateHatred
{
    [HarmonyTargetMethodHint("b1.BUS_BattleStateComp", "OnEnableCanUpdateHatred")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_BattleStateComp:OnEnableCanUpdateHatred");
    }

    public static bool Prefix(UActorCompBaseCS? __instance, bool bEnable)
    {
        if (__instance == null)
            return true;

        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        if (owner != null)
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                return true;
        }

        return !bEnable;
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.OnReset))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTamerOnReset
{
    static bool Prefix(EResetActorReason ResetReason, FTamerRef __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        Logging.LogDebug("Tamer on reset called for tamer {Tamer} with reason {Reason}", __instance.TamerName, ResetReason);
        return ResetReason != EResetActorReason.ReturnHome;
    }
}

[HarmonyPatch(typeof(BUS_FsmComp), "OnTriggerFsmEvent")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnTriggerFsmEvent
{
    public static bool Prefix(FGameplayTag EventTag, BUS_FsmComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (EventTag == BGW_FlowUtils.NormalAIFsmEventTag.LifeTimeGoHome)
        {
            return false;
        }

        if (EventTag == BGW_FlowUtils.NormalAIFsmEventTag.AIBattleAttack && DI.Instance.GameplayConfiguration.ShouldDisableTamerAttack())
        {
            return false;
        }

        var owner = __instance.GetOwner();
        if (EventTag == BGW_FlowUtils.NormalAIFsmEventTag.LifeTimeGazeAndSurround)
        {
            var anyPlayerAlive = false;
            DI.Instance.World.Query<MainCharacterComponent, HpComponent>().ForEachEntity((
                ref _, ref hp, _) =>
            {
                if (!hp.IsDead)
                    anyPlayerAlive = true;
            });

            if (anyPlayerAlive)
            {
                return false;
            }
        }

        if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(owner as BGUCharacterCS, out var entity))
        {
            Debug.Assert(owner == entity.Value.Pawn, "owner == tamerEntity.Pawn");
            if (!BGU_CommonUtil.IsInFsmState(owner, EventTag))
            {
                DI.Instance.MappedEvent.NotifyEcsIfApplicable(new TriggerFsmStateEvent(entity.Value, EventTag.TagName.ToString()), entity.Value.Entity);
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(BUS_MovementSystem), "TickForMonster")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchMovementTickForMonster
{
    public static void Postfix(float DeltaTime, bool bStopMove, bool bNeedPauseMoveModeUpdate, BUS_MovementSystem? __instance, BUC_MovementData ___MovementData)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (__instance == null)
        {
            Logging.LogError("__instance is null in BUC_ABPCharacterData.Update_GameThread");
            return;
        }

        var owner = __instance.GetOwner();
        if (owner is not BGUCharacterCS character)
            return;

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(character);
        if (tamerEntity.HasValue)
        {
            if (!tamerEntity.Value.IsTamerValid)
                return;

            ref var anim = ref tamerEntity.Value.GetMonsterAnimation();
            if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                anim.MoveAiType = (byte)___MovementData.MoveAIType;
            }
            else
            {
                var events = BUS_EventCollectionCS.Get(tamerEntity.Value.Pawn);
                events.Evt_SwitchMoveAIType.Invoke((EBGUMoveAIType)anim.MoveAiType);
            }
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.AfterMonsterDead))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchAfterMonsterDead
{
    public static void Prefix(FTamerRef? __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (__instance == null)
            return;

        if (__instance.Phase == ETamerPhase.Dead)
            return;

        var monster = __instance.MonsterInstancePtr.Get();
        if (monster == null)
            return;

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(monster);
        if (tamerEntity.HasValue)
        {
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
            ref var meta = ref tamerEntity.Value.GetMeta();
            localTamer.IsMonsterActive = false;
            MarkerUtils.DestroyMarkerForCharacter(tamerEntity.Value);
            TamerUtils.MarkMonsterLocallyDespawned(DI.Instance.MappedEvent, tamerEntity.Value);
            Logging.LogDebug("Unloading monster locally. NetId: {NetId}, guid {Guid} (MonsterDead)", meta.NetId, BGU_DataUtil.GetActorGuid(monster));
        }
    }
}

[HarmonyPatch(typeof(BUS_AIComp), "TriggerWakeupActivated")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTriggerWakeupActivated
{
    public static void Postfix(BUS_AIComp? __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (__instance == null)
            return;

        var owner = __instance.GetOwner();
        if (owner.IsNullOrDestroyed() || owner is not BGUCharacterCS character)
            return;

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(character);
        if (tamerEntity is not { IsTamerValid: true })
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new MonsterWakeUpEvent(tamerEntity.Value), tamerEntity.Value.Entity);
    }
}

[HarmonyPatch(typeof(BUS_DumperTruckTriggerComp), "PatrolTick")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchPatrolTick
{
    private static MethodInfo? _dumperTruckTriggerDataGetter;
    private static MethodInfo? _BeGetter;
    private static MethodInfo? _BeSetter;

    public static bool Prefix(BUS_DumperTruckTriggerComp? __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (__instance == null)
            return true;

        _dumperTruckTriggerDataGetter ??= AccessTools.PropertyGetter(typeof(BUS_DumperTruckTriggerComp), "DumperTruckTriggerData");
        _BeGetter ??= AccessTools.PropertyGetter(typeof(BUS_DumperTruckTriggerComp), "BE");
        _BeSetter ??= AccessTools.PropertySetter(typeof(BUS_DumperTruckTriggerComp), "BE");

        var dumperTruckTriggerData = (BUC_DumperTruckTriggerData)_dumperTruckTriggerDataGetter.Invoke(__instance, null);

        var character = dumperTruckTriggerData.ControlledUnit;
        if (character.IsNullOrDestroyed())
            return true;

        if ((BUS_GSEventCollection?)_BeGetter.Invoke(__instance, null) == null)
        {
            var be = BUS_EventCollectionCS.Get(BGU_DataUtil.GetActorByGuid(__instance.GetOwner(), dumperTruckTriggerData.UnitGuid));
            _BeSetter.Invoke(__instance, [be]);
        }

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(character);
        if (tamerEntity.HasValue)
        {
            if (!tamerEntity.Value.IsTamerValid)
                return true;

            ref var anim = ref tamerEntity.Value.GetMonsterAnimation();
            if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                anim.AnimationPlayRate = dumperTruckTriggerData.ControlledUnit.Mesh.GetPlayRate();
                return true;
            }

            // Run alternative patrol logic for non-owned monsters
            character.Mesh.SetPlayRate(anim.AnimationPlayRate);
            var playRateAbs = Math.Abs(anim.AnimationPlayRate);
            if (playRateAbs > dumperTruckTriggerData.DamageAvailableSpeedThreshold)
            {
                __instance.EnableSweepCheck();
                __instance.TriggerBeginEvent();
            }
            else if (playRateAbs < dumperTruckTriggerData.DamageDisableSpeedThreshold)
            {
                __instance.DisableSweepCheck();
                __instance.TriggerEndEvent();
            }

            return false;
        }

        return true;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class TamerTeamResetPatch
{
    [HarmonyTargetMethodHint("b1.BUS_TeamIDManageComp", "SetDefaultTeamIDInternal")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_TeamIDManageComp:SetDefaultTeamIDInternal");
    }

    public static bool Prefix(BGUCharacterCS ___OwnerAsCharacterCS)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var tamer = DI.Instance.PawnState.GetEntityByTamerMonster(___OwnerAsCharacterCS);

        if (tamer != null)
        {
            var team = tamer.Value.GetTeam().TeamId;
            if (tamer.Value.Pawn == ___OwnerAsCharacterCS && team != 0)
            {
                Logging.LogDebug("Prevented team ID reset for {Guid} with team ID {TeamId}", tamer.Value.GetTamer().Guid, team);
                return false;
            }
        }

        return true;
    }
}