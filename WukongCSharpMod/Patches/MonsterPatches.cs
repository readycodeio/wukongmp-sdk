using System.Reflection;
using b1;
using BtlShare;
using HarmonyLib;
using UnrealEngine.Runtime;

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
                    if (!state.IsSpawned)
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

    [HarmonyPatch(typeof(FTamerRef), "IncrementalBeginPlayUnit")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchTamerLoad
    {
        public static void Postfix(FTamerRef __instance)
        {
            if (!__instance.IsMonsterValid())
                return;

            var photon = MyMod.Instance.Photon;
            var tamer = __instance.InstancePtr.Get();
            var monster = __instance.MonsterInstancePtr.Get();
            var guid = BGU_DataUtil.GetActorGuid(monster);

            // register in Photon if not present
            var monsterState = photon.GetByTamerActor(tamer);
            if (monsterState == null)
            {
                monsterState = new MonsterState(guid, tamer);
                photon.SyncedMonsters.Add(guid, monsterState);

                // notify other clients
                photon.SendMonsterWakeUp(guid);
            }

            // sanity check guid
            if (monsterState.Guid != guid)
            {
                Helpers.LogError($"Guid mismatch: {monsterState.Guid} {guid}");
                return;
            }

            if (photon.IsMasterClient)
            {
                // subscribe to events on master
                var events = BUS_EventCollectionCS.Get(monsterState.Pawn);
                events.Evt_PlayMontageCallback += (reason, montage, state) =>
                {
                    var montagePath = montage.GetPathName();
                    Helpers.Log($"Monster montage callback: {monsterState.Guid} {reason} {montagePath} {state}");
                    photon.SendMonsterMontageCallback(monsterState.Guid, reason, montagePath, state);
                };

                // also, set HP
                var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(monster);
                monsterState.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
            }
            else
            {
                // disable AI on clients
                var events = BUS_EventCollectionCS.Get(monster);

                if (events is null)
                {
                    Helpers.LogError("Events is null");
                    return;
                }

                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                events.Evt_AIPauseFsm.Invoke(true);
                events.Evt_EnableCanUpdateHatred.Invoke(P1: false);
                events.Evt_EnableCanSetBT.Invoke(P1: false);

                Helpers.Log("Tamer actor disabled.");
            }

            // at this point the monster exists, so we set IsSpawned
            monsterState.IsSpawned = true;
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