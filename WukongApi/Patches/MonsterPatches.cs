using System;
using System.Reflection;
using b1;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Runtime;
using WukongApi.ECS;

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
                client.EntityManager.RunSystem((
                    EntityId _,
                    ref LocalTamerComponent tamer,
                    ref TranslationComponent trans
                ) =>
                {
                    if (!tamer.IsSynced || !tamer.IsTamerValid)
                        return;

                    trans.Position = tamer.Tamer!.GetActorLocation().ToVector3();
                    trans.Rotation = tamer.Tamer.GetActorRotation().ToVector3();
                });
            }
            else
            {
                client.EntityManager.RunSystem((
                    EntityId _,
                    ref LocalTamerComponent tamer,
                    ref TranslationComponent trans
                ) =>
                {
                    if (!tamer.IsTamerValid || !tamer.IsSynced)
                        return;

                    var events = BUS_EventCollectionCS.Get(tamer.Tamer);

                    if (events == null)
                        return;

                    var pos = trans.Position.ToFVector();
                    var rot = trans.Rotation.ToFRotator();

                    if (!pos.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance) && !pos.Equals(tamer.Tamer!.GetActorLocation(), Constants.FloatComparisonTolerance))
                    {
                        GameLoopPatch.QueueOnGameThread(() =>
                        {
                            events.Evt_InterpolationMove.Invoke(pos, rot, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                        });
                    }
                });
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
                var entity = client.GetByTamerActor(tamer);
                if (entity != null)
                {
                    ref var tamerComp = ref client.GetEntityComponent<TamerComponent>(entity.Value);
                    tamerComp.IsSpawned = true; 
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
                var entity = client.GetMonsterByActor(owner);
                if (entity != null)
                {
                    var tamerComp = client.GetEntityComponent<LocalTamerComponent>(entity.Value);
                    if (tamerComp.Pawn != null && !BGU_CommonUtil.IsInFsmState(tamerComp.Pawn, EventTag))
                    {
                        Logging.LogDebug("Sending fsm state {State} for {Actor}", EventTag.ToString(), owner.GetName());
                        var netPeer = client.GetEntityComponent<NetworkIdComponent>(entity.Value);
                        client.SendTriggerFsmState(netPeer, EventTag);
                    }
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

            var entity = client.GetMonsterByCharacter(character);
            if (entity.HasValue)
            {
                ref var tamerComp = ref client.GetEntityComponent<LocalTamerComponent>(entity.Value);
                
                if (!tamerComp.IsTamerValid)
                    return;
                
                ref var anim = ref client.GetEntityComponent<MonsterAnimationComponent>(entity.Value);
                if (client.IsMasterClient)
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