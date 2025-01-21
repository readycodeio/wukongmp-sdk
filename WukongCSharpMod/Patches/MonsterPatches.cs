using System.Reflection;
using b1;
using CSharpModBase;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongCSharpMod.State;

namespace WukongCSharpMod.Patches
{
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
                foreach (var (id, state) in photon.SyncedMonsters)
                {
                    var events = BUS_EventCollectionCS.Get(state.Pawn);

                    if (!state.Location.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance) && !state.Location.Equals(state.Pawn.GetActorLocation(), Constants.FloatComparisonTolerance))
                    {
                        Utils.TryRunOnGameThread(() => events.Evt_InterpolationMove.Invoke(state.Location, state.Rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true));
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