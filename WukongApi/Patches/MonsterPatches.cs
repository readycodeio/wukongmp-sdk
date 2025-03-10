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
            var photon = WukongMP.Instance.Photon;

            if (photon.IsMasterClient)
            {
                foreach (var (id, state) in photon.SyncedMonsters)
                {
                    // sync location
                    if (!state.IsSynced)
                        continue;

                    if (!state.IsTamerValid)
                        continue;

                    var location = state.Pawn.GetActorLocation();
                    if (!location.Equals(state.Location, Constants.FloatComparisonTolerance))
                    {
                        state.Location = location;
                        photon.CacheMonsterProperty(id, nameof(MonsterState.Location), state.Location);
                    }

                    var rotation = state.Pawn.GetActorRotation();
                    if (!rotation.Equals(state.Rotation, Constants.FloatComparisonTolerance))
                    {
                        state.Rotation = rotation;
                        photon.CacheMonsterProperty(id, nameof(MonsterState.Rotation), state.Rotation);
                    }
                }
            }
            else
            {
                foreach (var state in photon.SyncedMonsters.Values)
                {
                    if (!state.IsTamerValid || !state.IsSynced)
                        continue;

                    var events = BUS_EventCollectionCS.Get(state.Pawn);

                    if (events == null)
                    {
                        // Logging.LogWarning($"BUS_EventCollectionCS is null for monster {}", state.Pawn.GetName());
                        continue;
                    }

                    if (!state.Location.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance) && !state.Location.Equals(state.Pawn.GetActorLocation(), Constants.FloatComparisonTolerance))
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

                var photon = WukongMP.Instance.Photon;
                var tamer = __instance.InstancePtr.Get();

                Logging.LogDebug("Monster {Guid} waking up locally", BGU_DataUtil.GetActorGuid(tamer.GetMonster()));
                PhotonUtils.SyncMonsterAndNotify(photon, tamer);
            }
            catch (Exception e)
            {
                // print and ignore
                Logging.LogError("Error in PatchTamerLoad.Postfix");
                Logging.LogError(e.Message);
                Logging.LogError(e.StackTrace);
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

            if (WukongMP.Instance.Photon.IsMasterClient)
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

            if (WukongMP.Instance.Photon.IsMasterClient)
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

            if (WukongMP.Instance.Photon.IsMasterClient)
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

            if (WukongMP.Instance.Photon.IsMasterClient)
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

            if (WukongMP.Instance.Photon.IsMasterClient)
                return true;

            return !bEnable;
        }
    }
}