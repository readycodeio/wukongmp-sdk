using System.Reflection;
using b1;
using HarmonyLib;
using UnrealEngine.Engine;
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
                        events.Evt_InterpolationMove.Invoke(state.Location, state.Rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(BUC_ABPAdvancedMonsterLocomotionData), nameof(BUC_ABPAdvancedMonsterLocomotionData.Update))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchMonsterLocomotion
    {
        public static void Postfix(BUC_ABPAdvancedMonsterLocomotionData __instance,
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
            // sync GaitGroundedState
            var photon = MyMod.Instance.Photon;

            if (!(Owner is BGUCharacterCS monster))
                return;

            var state = photon.GetMonsterByCharacter(monster);

            if (state is null)
                return;

            if (photon.IsMasterClient)
            {
                if (state.GaitGroundedState != __instance.GaitGroundedState)
                {
                    state.GaitGroundedState = __instance.GaitGroundedState;
                    photon.SetMonsterProperty(state.Guid, nameof(MonsterState.GaitGroundedState), state.GaitGroundedState);
                }

                if (state.GaitGroundedStateTemp != __instance.GaitGroundedStateTemp)
                {
                    state.GaitGroundedStateTemp = __instance.GaitGroundedStateTemp;
                    photon.SetMonsterProperty(state.Guid, nameof(MonsterState.GaitGroundedStateTemp), state.GaitGroundedStateTemp);
                }

                if (state.MoveGaitGroundedState != __instance.MoveGaitGroundedState)
                {
                    state.MoveGaitGroundedState = __instance.MoveGaitGroundedState;
                    photon.SetMonsterProperty(state.Guid, nameof(MonsterState.MoveGaitGroundedState), state.MoveGaitGroundedState);
                }
            }
            else
            {
                __instance.GaitGroundedState = state.GaitGroundedState;
                __instance.GaitGroundedStateTemp = state.GaitGroundedStateTemp;
                __instance.MoveGaitGroundedState = state.MoveGaitGroundedState;
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