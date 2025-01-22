using System;
using System.Reflection;
using b1;
using CSharpModBase;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongCSharpMod.State;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(FTamerRef), "IncrementalBeginPlayUnit")]
    public class PatchIncrementalBeginPlay
    {
        public static Exception Finalizer(Exception __exception, FTamerRef __instance)
        {
            Helpers.LogError("---------- IGNORING EXCEPTION ----------");
            Helpers.LogError(__exception.Message);
            Helpers.LogError("-------------- TAMER INFO --------------");
            Helpers.LogError($"Name: {__instance.TamerName}");
            Helpers.LogError($"Phase: {__instance.Phase.ToString()}");
            Helpers.LogError($"Tamer type: {__instance.TamerType}");
            Helpers.LogError($"Spawn rule: {__instance.SpawnRuleFlags}");
            Helpers.LogError($"Monster valid: {__instance.IsMonsterValid()}");
            Helpers.LogError($"Monster destroyed: {__instance.IsMonsterDestroyed()}");
            Helpers.LogError($"Instance valid: {__instance.InstancePtr.IsValid()}");
            Helpers.LogError("----------------------------------------");
            return null;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchTamerManagerTick
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BGS_TamerManagerSystem:OnTickWithGroup");
        }

        private static void Postfix(float DeltaTime, int TickGroup)
        {
            // send updates for each monster
            var photon = MyMod.Instance.Photon;

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
                        photon.SetMonsterProperty(id, nameof(MonsterState.Location), state.Location);
                    }

                    var rotation = state.Pawn.GetActorRotation();
                    if (!rotation.Equals(state.Rotation, Constants.FloatComparisonTolerance))
                    {
                        state.Rotation = rotation;
                        photon.SetMonsterProperty(id, nameof(MonsterState.Rotation), state.Rotation);
                    }
                }
            }
            else
            {
                foreach (var state in photon.SyncedMonsters.Values)
                {
                    if (!state.IsTamerValid)
                        continue;

                    var events = BUS_EventCollectionCS.Get(state.Pawn);

                    if (events == null)
                    {
                        Helpers.LogError($"BUS_EventCollectionCS is null for monster {state.Pawn.GetName()}");
                        continue;
                    }

                    if (!state.Location.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance) && !state.Location.Equals(state.Pawn.GetActorLocation(), Constants.FloatComparisonTolerance))
                    {
                        Utils.TryRunOnGameThread(() => { events.Evt_InterpolationMove.Invoke(state.Location, state.Rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true); });
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(FTamerRef), "IncrementalBeginPlayUnit")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchTamerLoad
    {
        public static void Postfix(FTamerRef __instance)
        {
            if (!__instance.IsMonsterValid() || !__instance.InstancePtr.IsValid())
                return;

            var photon = MyMod.Instance.Photon;
            var tamer = __instance.InstancePtr.Get();

            Helpers.Log($"Monster {BGU_DataUtil.GetActorGuid(tamer.GetMonster())} waking up locally");
            PhotonUtils.SyncMonsterAndNotify(photon, tamer);
        }
    }

    [HarmonyPatch(typeof(FTamerRef), nameof(FTamerRef.CanTurnBack2Loaded))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchTurnBack2Loaded
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(BUS_AIComp), "OnAIPerceptionSetting")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchOnAIPerceptionSetting
    {
        public static bool Prefix(bool bEnable)
        {
            if (MyMod.Instance.Photon.IsMasterClient)
                return true;

            return !bEnable;
        }
    }

    [HarmonyPatch(typeof(BUS_AIComp), "OnAIPauseBT")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchOnAIPauseBT
    {
        public static bool Prefix(bool IsPause)
        {
            if (MyMod.Instance.Photon.IsMasterClient)
                return true;

            return IsPause;
        }
    }


    [HarmonyPatch(typeof(BUS_AIComp), "OnEnableCanSetBT")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchOnEnableCanSetBT
    {
        public static bool Prefix(bool bEnable)
        {
            if (MyMod.Instance.Photon.IsMasterClient)
                return true;

            return !bEnable;
        }
    }

    [HarmonyPatch(typeof(BUS_FsmComp), "OnAIPauseFsm")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchOnAIPauseFsm
    {
        public static bool Prefix(bool IsPause)
        {
            if (MyMod.Instance.Photon.IsMasterClient)
                return true;

            return IsPause;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchOnEnableCanUpdateHatred
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BattleStateComp:OnEnableCanUpdateHatred");
        }

        public static bool Prefix(bool bEnable)
        {
            if (MyMod.Instance.Photon.IsMasterClient)
                return true;

            return !bEnable;
        }
    }
}