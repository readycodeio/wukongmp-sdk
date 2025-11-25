using System;
using System.Linq;
using System.Reflection;
using b1;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

// TODO: Duplication of character patch?
[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTamerManagerTick
{
    [HarmonyTargetMethodHint("b1.BGS_TamerManagerSystem", "OnTickWithGroup")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_TamerManagerSystem:OnTickWithGroup");
    }

    private static void Postfix(float DeltaTime, int TickGroup)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        DI.Instance.World.Query<LocalTamerComponent, TranslationComponent>().ForEachEntity((
            ref localTamerComp,
            ref transComp, entity) =>
        {
            if (!localTamerComp.IsTamerSynced || !localTamerComp.IsTamerValid || localTamerComp.Pawn == null)
                return;

            if (DI.Instance.ClientOwnership.OwnsEntity(entity))
            {
                // send updates for owned monsters
                transComp.Position = localTamerComp.Pawn.GetActorLocation().ToVector3();
                transComp.Rotation = localTamerComp.Pawn.GetActorRotation().ToVector3();
            }
            else
            {
                // apply updates for monsters owned by other players
                var events = BUS_EventCollectionCS.Get(localTamerComp.Pawn);

                if (events == null)
                    return;

                var pos = transComp.Position.ToFVector();
                var rot = transComp.Rotation.ToFRotator();

                var posChanged = !pos.Equals(localTamerComp.Pawn.GetActorLocation(), Constants.FloatComparisonTolerance);
                var rotChanged = !rot.Equals(localTamerComp.Pawn.GetActorRotation(), Constants.FloatComparisonTolerance);

                if (posChanged || rotChanged)
                {
                    events.Evt_InterpolationMove.Invoke(pos, rot, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                }
            }
        });
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnRegisterTamer
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTamerBeginPlayCS_Implementation
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
                    SpawningUtils.CreateMonsterInEcs(guid, __instance, Constants.DefaultMonsterTeamId, __instance.PathName);
                }
                else
                {
                    Logging.LogDebug("Monster already exists in ECS: {NetId}, guid: {Guid}", tamerEntity.Value.GetMeta().NetId, tamerEntity.Value.GetTamer().Guid);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), "IncrementalBeginPlayUnit")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTamerLoad
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
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
            var metadata = tamerEntity.Value.GetMeta();
            TamerUtils.MarkMonsterLocallySpawned(ref localTamer, metadata);
        }
        else if (!EcsExcludedMonsters.MonsterNames.Any(monsterGuid.Contains))
        {
            Logging.LogError("Spawned monster is not in the ECS, guid: {Guid}", monsterGuid);
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.CanTurnBack2Loaded))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchCanTurnBack2Loaded
{
    static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.TurnBack2Loaded))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTurnBack2Loaded
{
    static bool Prefix(FTamerRef __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (!__instance.IsMonsterValid() || !__instance.InstancePtr.IsValid())
            return true;

        var tamerActor = __instance.InstancePtr.Get();

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamer(tamerActor);
        if (tamerEntity.HasValue)
        {
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
            ref var meta = ref tamerEntity.Value.GetMeta();
            TamerUtils.MarkMonsterLocallyDespawned(ref localTamer, meta);
            localTamer.HasPendingUnload = true;

            ref var tamer = ref tamerEntity.Value.GetTamer();
            if (!tamer.ShouldBeSpawned)
            {
                Logging.LogDebug("Unloading monster {Guid} locally", BGU_DataUtil.GetActorGuid(tamerActor));
                localTamer.IsMonsterActive = false;
                localTamer.HasPendingUnload = false;
                MarkerUtils.DestroyMarkerForCharacter(tamerEntity.Value);
                return true;
            }

            return false;
        }
        else
        {
            Logging.LogError("Unloading monster is not in the ECS, guid: {Guid}", BGU_DataUtil.GetActorGuid(tamerActor.GetMonster()));
            return true;
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), "DestroyTamer")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTamerUnload
{
    public static void Prefix(FTamerRef __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (__instance.TamerType == ETamerType.Summoned || (__instance.TamerType == ETamerType.Spawned && DI.Instance.GameplayConfiguration.EnableSpawnedTamers))
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamer(__instance.InstancePtr.Value);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                tamerEntity.Value.GetLocalTamer().Tamer = null;
                Logging.LogDebug("Deleting tamer entity from ECS: id {Entity} (DestroyTamer)", tamerEntity.Value.GetMeta().NetId);
                DI.Instance.EcsLoop.CommandBuffer.DeleteEntity(tamerEntity.Value.Entity.Id);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_AIComp), "OnAIPerceptionSetting")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnAIPerceptionSetting
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnAIPauseBT
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnEnableCanSetBT
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnAIPauseFsm
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
                tamerEntity.Value.GetTamer().HasFsmEnabled = !IsPause;
                return true;
            }
        }

        return IsPause;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnEnableCanUpdateHatred
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

/// <summary>
/// Only reset character Team ID if it was not set by us.
/// This prevents the game from resetting the team ID of monsters assigned to player teams in PvP.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class TamerResetPatch
{
    [HarmonyTargetMethodHint("b1.BUS_TeamIDManageComp", "OnResetTeamID")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_TeamIDManageComp:OnResetTeamID");
    }

    public static bool Prefix(BGUCharacterCS ___OwnerAsCharacterCS)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var teamId = ___OwnerAsCharacterCS.GetTeamIDInCS();
        return !Constants.AvailableTeamIds.Contains(teamId);
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.OnReset))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTamerOnReset
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnTriggerFsmEvent
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
            DI.Instance.World.Query<MainCharacterComponent>().ForEachEntity((
                ref playerComp, _) =>
            {
                if (!playerComp.IsDead)
                    anyPlayerAlive = true;
            });

            if (anyPlayerAlive)
            {
                return false;
            }
        }

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
        if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
        {
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
            if (localTamer.Pawn != null && !BGU_CommonUtil.IsInFsmState(localTamer.Pawn, EventTag))
            {
                var netId = tamerEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendTriggerFsmState(new FsmStateData(netId, EventTag.TagName.ToString()));
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(BUS_MovementSystem), "TickForMonster")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchMovementTickForMonster
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
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();

            if (!localTamer.IsTamerValid)
                return;

            ref var anim = ref tamerEntity.Value.GetMonsterAnimation();
            if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                anim.MoveAiType = (byte)___MovementData.MoveAIType;
            }
            else
            {
                var events = BUS_EventCollectionCS.Get(localTamer.Pawn);
                events.Evt_SwitchMoveAIType.Invoke((EBGUMoveAIType)anim.MoveAiType);
            }
        }
    }
}

[HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.AfterMonsterDead))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchAfterMonsterDead
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
            TamerUtils.MarkMonsterLocallyDespawned(ref tamerEntity.Value.GetLocalTamer(), tamerEntity.Value.GetMeta());
            Logging.LogDebug("Unloading monster locally. NetId: {NetId}, guid {Guid} (MonsterDead)", meta.NetId, BGU_DataUtil.GetActorGuid(monster));
        }
    }
}

[HarmonyPatch(typeof(BUS_AIComp), "TriggerWakeupActivated")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchTriggerWakeupActivated
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
        if (tamerEntity.HasValue)
        {
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
            if (!localTamer.IsTamerValid)
                return;

            if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                DI.Instance.Rpc.SendMonsterWakeUp(tamerEntity.Value.GetMeta().NetId);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_DumperTruckTriggerComp), "PatrolTick")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchPatrolTick
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
            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
            if (!localTamer.IsTamerValid)
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