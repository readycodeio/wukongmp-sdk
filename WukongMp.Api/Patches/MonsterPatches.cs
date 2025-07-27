using b1;
using HarmonyLib;
using System;
using System.Reflection;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchTamerManagerTick
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BGS_TamerManagerSystem:OnTickWithGroup");
        }

        private static void Postfix(float DeltaTime, int TickGroup)
        {
            if (!DI.Instance.RoomState.InRoom)
                return;

            DI.Instance.World.Query<MetadataComponent, LocalTamerComponent, TranslationComponent>().ForEachEntity((ref meta, ref tamer, ref trans, _) =>
            {
                if (!tamer.IsTamerSynced || !tamer.IsTamerValid || tamer.Pawn == null)
                    return;

                if (DI.Instance.OwnsEntity(meta.Owner))
                {
                    // send updates for owned monsters
                    trans.Position = tamer.Pawn.GetActorLocation().ToVector3();
                    trans.Rotation = tamer.Pawn.GetActorRotation().ToVector3();
                }
                else
                {
                    // apply updates for monsters owned by other players
                    var events = BUS_EventCollectionCS.Get(tamer.Pawn);

                    if (events == null)
                        return;

                    var pos = trans.Position.ToFVector();
                    var rot = trans.Rotation.ToFRotator();

                    var posChanged = !pos.Equals(tamer.Pawn.GetActorLocation(), Constants.FloatComparisonTolerance);
                    var rotChanged = !rot.Equals(tamer.Pawn.GetActorRotation(), Constants.FloatComparisonTolerance);

                    if (posChanged || rotChanged)
                    {
                        GameLoopPatch.QueueOnGameThread(() => { events.Evt_InterpolationMove.Invoke(pos, rot, Constants.ToleratedLatencyMs / 1000f, true, false, false, true); });
                    }
                }
            });
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnRegisterTamer
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BGS_TamerManagerSystem:OnRegisterTamer");
        }

        public static void Postfix(FTamerRef InTamer)
        {
            if (!DI.Instance.RoomState.InRoom)
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
            if (!DI.Instance.RoomState.InRoom)
                return;

            if (DI.Instance.RoomState.IsMasterClient)
            {
                if (__instance.TamerType != ETamerType.Summoned)
                {
                    var guid = BGU_DataUtil.GetActorGuid(__instance);
                    var entity = DI.Instance.PawnRegistry.GetMonsterByGuid(guid);
                    if (entity == null)
                    {
                        SpawningUtils.CreateMonsterInEcs(guid, __instance, 2, __instance.PathName);
                    }
                    else
                    {
                        Logging.LogDebug("Monster already exists in ECS: {Entity}", entity.ToString());
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
            if (!DI.Instance.RoomState.InRoom)
                return;

            try
            {
                if (!__instance.IsMonsterValid() || !__instance.InstancePtr.IsValid())
                    return;

                var tamer = __instance.InstancePtr.Get();

                Logging.LogDebug("Monster {Guid} waking up locally", BGU_DataUtil.GetActorGuid(tamer));
                var entity = DI.Instance.PawnRegistry.GetByTamerActor(tamer);
                if (entity.HasValue)
                {
                    ref var localTamerComp = ref entity.Value.GetComponent<LocalTamerComponent>();
                    if (!localTamerComp.IsLocallySpawned)
                    {
                        localTamerComp.IsLocallySpawned = true;
                        var meta = entity.Value.GetComponent<MetadataComponent>();
                        DI.Instance.Rpc.SendUnitSpawned(meta.NetId);
                    }
                }
                else
                {
                    Logging.LogError("Spawned monster is not in the ECS, guid: {Guid}", BGU_DataUtil.GetActorGuid(tamer.GetMonster()));
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e);
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
    [HarmonyPatchCategory(Constants.CoopPatches)]
    public class PatchTurnBack2Loaded
    {
        static bool Prefix(FTamerRef __instance)
        {
            if (!DI.Instance.RoomState.InRoom)
                return true;

            if (!__instance.IsMonsterValid() || !__instance.InstancePtr.IsValid())
                return true;

            var tamer = __instance.InstancePtr.Get();

            var entity = DI.Instance.PawnRegistry.GetByTamerActor(tamer);
            if (entity.HasValue)
            {
                ref var localTamerComp = ref entity.Value.GetComponent<LocalTamerComponent>();
                if (localTamerComp.IsLocallySpawned)
                {
                    localTamerComp.IsLocallySpawned = false;
                    var meta = entity.Value.GetComponent<MetadataComponent>();
                    DI.Instance.Rpc.SendUnitDespawn(meta.NetId);
                }

                ref var tamerComp = ref entity.Value.GetComponent<TamerComponent>();
                if (!tamerComp.ShouldBeSpawned)
                {
                    Logging.LogDebug("Unloading monster {Guid} locally", BGU_DataUtil.GetActorGuid(tamer));
                    localTamerComp.IsMonsterSynced = false;
                    return true;
                }

                return false;
            }
            else
            {
                Logging.LogError("Unloading monster is not in the ECS, guid: {Guid}", BGU_DataUtil.GetActorGuid(tamer.GetMonster()));
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(BUS_AIComp), "OnAIPerceptionSetting")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnAIPerceptionSetting
    {
        public static bool Prefix(BUS_AIComp __instance, bool bEnable)
        {
            if (!DI.Instance.RoomState.InRoom)
                return true;

            var owner = __instance.GetOwner();
            if (owner != null)
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

                if (entity.HasValue && DI.Instance.OwnsEntity(entity.Value))
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
            if (!DI.Instance.RoomState.InRoom)
                return true;

            var owner = __instance.GetOwner();
            if (owner != null)
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

                if (entity.HasValue && DI.Instance.OwnsEntity(entity.Value))
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
            if (!DI.Instance.RoomState.InRoom)
                return true;

            var owner = __instance.GetOwner();
            if (owner != null)
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

                if (entity.HasValue && DI.Instance.OwnsEntity(entity.Value))
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
            if (!DI.Instance.RoomState.InRoom)
                return true;

            var owner = __instance.GetOwner();
            if (owner != null)
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

                if (entity.HasValue && DI.Instance.OwnsEntity(entity.Value))
                    return true;
            }

            return IsPause;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnEnableCanUpdateHatred
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BattleStateComp:OnEnableCanUpdateHatred");
        }

        public static bool Prefix(UActorCompBaseCS __instance, bool bEnable)
        {
            if (!DI.Instance.RoomState.InRoom)
                return true;

            var owner = __instance.GetOwner();
            if (owner != null)
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

                if (entity.HasValue && DI.Instance.OwnsEntity(entity.Value))
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
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_TeamIDManageComp:OnResetTeamID");
        }

        public static bool Prefix(UActorCompBaseCS __instance)
        {
            if (!DI.Instance.RoomState.InRoom)
                return true;

            var teamId = Traverse.Create(__instance).Field<BGUCharacterCS>("OwnerAsCharacterCS").Value.GetTeamIDInCS();
            return !Constants.AvailableTeamIds.Contains(teamId);
        }
    }

    [HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.OnReset))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchTamerOnReset
    {
        static bool Prefix(EResetActorReason ResetReason, FTamerRef __instance)
        {
            if (!DI.Instance.RoomState.InRoom)
                return true;

            Logging.LogDebug("Tamer on reset called for tamer {Tamer} with reason {Reason}", __instance.TamerName, ResetReason);
            return ResetReason != EResetActorReason.ReturnHome && ResetReason != EResetActorReason.InteractRebirthPoint;
        }
    }

    [HarmonyPatch(typeof(BUS_FsmComp), "OnTriggerFsmEvent")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnTriggerFsmEvent
    {
        public static bool Prefix(FGameplayTag EventTag, BUS_FsmComp __instance)
        {
            if (!DI.Instance.RoomState.InRoom)
                return true;

            if (EventTag == BGW_FlowUtils.NormalAIFsmEventTag.LifeTimeGoHome)
            {
                Logging.LogTrace("Trying change state to {State}", EventTag.ToString());
                return false;
            }

            var owner = __instance.GetOwner();
            var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);
            if (entity.HasValue && DI.Instance.OwnsEntity(entity.Value))
            {
                var tamerComp = entity.Value.GetComponent<LocalTamerComponent>();
                if (tamerComp.Pawn != null && !BGU_CommonUtil.IsInFsmState(tamerComp.Pawn, EventTag))
                {
                    Logging.LogDebug("Sending fsm state {State} for {Actor}", EventTag.ToString(), owner.GetName());
                    var netId = entity.Value.GetComponent<MetadataComponent>().NetId;
                    DI.Instance.Rpc.SendTriggerFsmState(new FsmStateData(netId, EventTag.TagName.ToString()));
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BUS_MovementSystem), "TickForMonster")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchMovementTickForMonstere
    {
        public static void Postfix(float DeltaTime, bool bStopMove, bool bNeedPauseMoveModeUpdate, BUS_MovementSystem? __instance, BUC_MovementData ___MovementData)
        {
            if (!DI.Instance.RoomState.InRoom)
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

            var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);
            if (entity.HasValue)
            {
                ref var tamerComp = ref entity.Value.GetComponent<LocalTamerComponent>();

                if (!tamerComp.IsTamerValid)
                    return;

                ref var anim = ref entity.Value.GetComponent<MonsterAnimationComponent>();
                if (DI.Instance.OwnsEntity(entity.Value))
                {
                    anim.MoveAiType = (byte)___MovementData.MoveAIType;
                }
                else
                {
                    var events = BUS_EventCollectionCS.Get(tamerComp.Pawn);
                    events.Evt_SwitchMoveAIType.Invoke((EBGUMoveAIType)anim.MoveAiType);
                }
            }
        }
    }
}