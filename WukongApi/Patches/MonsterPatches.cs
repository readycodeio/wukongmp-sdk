using System;
using System.Reflection;
using b1;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongApi.State;

namespace WukongApi.Patches
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            // send updates for each monster
            var client = WukongMP.Instance.Client;

            if (client.IsMasterClient)
            {
                foreach (var (id, state) in client.SyncedMonsters)
                {
                    // sync location
                    if (!state.IsSynced)
                        continue;

                    if (!state.IsTamerValid)
                        continue;

                    if (state.Tamer == null)
                    {
                        Logging.LogError("Monster tamer is null");
                        continue;
                    }

                    var location = state.Tamer.GetActorLocation();
                    if (!location.Equals(state.Location, Constants.FloatComparisonTolerance))
                    {
                        state.Location = location;
                        client.CacheMonsterProperty(id, nameof(MonsterState.Location), state.Location);
                    }

                    var rotation = state.Tamer.GetActorRotation();
                    if (!rotation.Equals(state.Rotation, Constants.FloatComparisonTolerance))
                    {
                        state.Rotation = rotation;
                        client.CacheMonsterProperty(id, nameof(MonsterState.Rotation), state.Rotation);
                    }
                }
            }
            else
            {
                foreach (var state in client.SyncedMonsters.Values)
                {
                    if (!state.IsTamerValid || !state.IsSynced)
                        continue;

                    var events = BUS_EventCollectionCS.Get(state.Tamer);

                    if (events == null)
                        continue;

                    if (state.Tamer == null)
                    {
                        Logging.LogError("Monster tamer is null");
                        continue;
                    }

                    if (!state.Location.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance) && !state.Location.Equals(state.Tamer.GetActorLocation(), Constants.FloatComparisonTolerance))
                    {
                        GameLoopPatch.QueueOnGameThread(() => { events.Evt_InterpolationMove.Invoke(state.Location, state.Rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true); });
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            try
            {
                if (!__instance.IsMonsterValid() || !__instance.InstancePtr.IsValid())
                    return;

                var client = WukongMP.Instance.Client;
                var tamer = __instance.InstancePtr.Get();

                Logging.LogDebug("Monster {Guid} waking up locally", BGU_DataUtil.GetActorGuid(tamer.GetMonster()));
                ClientUtils.SyncMonsterAndNotify(client, tamer);
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
        }
    }

    [HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.CanTurnBack2Loaded))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchTurnBack2Loaded
    {
        static bool Prefix(ref bool __result)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(BUS_AIComp), "OnAIPerceptionSetting")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnAIPerceptionSetting
    {
        public static bool Prefix(bool bEnable)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            if (WukongMP.Instance.Client.IsMasterClient)
                return true;

            return !bEnable;
        }
    }

    [HarmonyPatch(typeof(BUS_AIComp), "OnAIPauseBT")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnAIPauseBT
    {
        public static bool Prefix(bool IsPause)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            if (WukongMP.Instance.Client.IsMasterClient)
                return true;

            return IsPause;
        }
    }


    [HarmonyPatch(typeof(BUS_AIComp), "OnEnableCanSetBT")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnEnableCanSetBT
    {
        public static bool Prefix(bool bEnable)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            if (WukongMP.Instance.Client.IsMasterClient)
                return true;

            return !bEnable;
        }
    }

    [HarmonyPatch(typeof(BUS_FsmComp), "OnAIPauseFsm")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnAIPauseFsm
    {
        public static bool Prefix(bool IsPause)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            if (WukongMP.Instance.Client.IsMasterClient)
                return true;

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

        public static bool Prefix(bool bEnable)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            if (WukongMP.Instance.Client.IsMasterClient)
                return true;

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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            if (EventTag == BGW_FlowUtils.NormalAIFsmEventTag.LifeTimeGoHome)
            {
                Logging.LogDebug("Trying change state to {State}", EventTag.ToString());
                return false;
            }

            var client = WukongMP.Instance.Client;
            if (client.IsMasterClient)
            {
                var owner = __instance.GetOwner();
                var character = client.GetMonsterByActor(owner);
                if (character != null)
                {
                    Logging.LogDebug("Sending fsm state {State} for {Actor}", EventTag.ToString(), owner.GetName());
                    client.SendTriggerFsmState(character.PeerId, EventTag);
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BUS_MovementSystem), "TickForMonster")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchMovementTickForMonstere
    {
        public static void Postfix(float DeltaTime, bool bStopMove, bool bNeedPauseMoveModeUpdate, BUS_MovementSystem __instance, BUC_MovementData ___MovementData)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
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

            var client = WukongMP.Instance.Client;

            var monsterState = client.GetMonsterByCharacter(character);
            if (monsterState is { IsSynced: true })
            {
                if (client.IsMasterClient)
                {
                    if (monsterState.MoveAIType != ___MovementData.MoveAIType)
                    {
                        monsterState.MoveAIType = ___MovementData.MoveAIType;
                        Logging.LogWarning("Move AI type changed to {State} for {Actor}", monsterState.MoveAIType, owner.GetName());
                        client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveAIType), monsterState.MoveAIType);
                    }
                }
                else
                {
                    var events = BUS_EventCollectionCS.Get(monsterState.Pawn);
                    events.Evt_SwitchMoveAIType.Invoke(monsterState.MoveAIType);
                }
            }
        }
    }
}