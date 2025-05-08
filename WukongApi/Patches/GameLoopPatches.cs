using System;
using System.Collections.Concurrent;
using b1;
using b1.ECS;
using HarmonyLib;
using WukongApi.Monitors;
using WukongApi.State;

namespace WukongApi.Patches
{
    public static class GameLoopPatch
    {
        public static readonly ConcurrentDictionary<BGW_TickGroupMask, ConcurrentQueue<(Action Action, string? Name)>> CustomTickGroupActionQueues = new();

        public static void QueueOnGameThread(Action action, string? name = null, BGW_TickGroupMask tickGroup = BGW_TickGroupMask.TG_OnTick)
        {
            if (name != null)
            {
                Logging.LogTrace("Enqueueing action: {Action}", name);
            }

            CustomTickGroupActionQueues.AddOrUpdate(tickGroup, _ => new ConcurrentQueue<(Action, string?)>([(action, name)]), (_, queue) =>
            {
                queue.Enqueue((action, name));
                return queue;
            });
        }

        public static BGW_TickGroupMask CustomTickGroupToTickGroupMask(int tickGroup)
        {
            switch (tickGroup)
            {
                case 0:
                    return BGW_TickGroupMask.TG_OnTick;
                case 1:
                    return BGW_TickGroupMask.TG_None;
                case 2:
                    return BGW_TickGroupMask.TG_AfterAnim;
                case 3:
                    return BGW_TickGroupMask.TG_None;
                case 4:
                    return BGW_TickGroupMask.TG_PostPhysics;
                case 5:
                    return BGW_TickGroupMask.TG_PostUpdateWork;
                case 101:
                    return BGW_TickGroupMask.TG_PreAnim;
                case 111:
                    return BGW_TickGroupMask.TG_BeforeStartPhsic;
                case 141:
                    return BGW_TickGroupMask.TG_BeforePostPhsic;
                case 151:
                    return BGW_TickGroupMask.TG_BeforePostUpdateWork;
                default:
                    Logging.LogError("CustomTickGroup_To_BGWTickGroupMask : unknown tickgroup");
                    return BGW_TickGroupMask.TG_None;
            }
        }
    }

    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public static class ReceiveTickPatch
    {
        public static void Postfix(int TickGroup)
        {
            var mask = GameLoopPatch.CustomTickGroupToTickGroupMask(TickGroup);

            if (mask is BGW_TickGroupMask.TG_PreTick
                or BGW_TickGroupMask.TG_OnTick
                or BGW_TickGroupMask.TG_LateTick
                or BGW_TickGroupMask.TG_ThreadTick)
                return;

            if (!GameLoopPatch.CustomTickGroupActionQueues.TryGetValue(mask, out var queue))
                return;

            while (queue.TryDequeue(out var item))
            {
                try
                {
                    Logging.LogTrace("Processing {Action} action for tick group {Mask}", item.Name, mask);
                    item.Action();
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                }
            }
        }
    }

    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class MontageSyncPatch
    {
        public static void Postfix()
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;
            
            SyncMontage(client.LocalPlayerState);
            if (client.IsMasterClient)
            {
                foreach (var monsterState in client.SyncedMonsters.Values)
                {
                    SyncMontage(monsterState);
                }
            }
        }

        private static void SyncMontage(CharacterState characterState)
        {
            if (characterState.Pawn == null)
                return;

            var montageState = characterState.MontageState;
            if (montageState.LocalAnimationInstance == null)
            {
                montageState.LocalAnimationInstance = characterState.Pawn.Mesh.GetAnimInstance();
            }

            var currentMontage = characterState.Pawn.GetCurrentMontage();

            if (currentMontage != null)
            {
                bool isNewMontage = montageState.LocalMontage != currentMontage;
                float currentPosition = montageState.LocalAnimationInstance.Montage_GetPosition(currentMontage);

                bool hasMontageRewound = currentPosition < montageState.LocalMontagePosition && !isNewMontage;
                bool hasSkippedFrames = currentPosition - montageState.LocalMontagePosition > 0.5f && !isNewMontage;

                if (isNewMontage || hasMontageRewound || hasSkippedFrames)
                {
                    WukongMP.Instance.Client.SendMontageCallback(characterState.PeerId, currentMontage, currentPosition, hasMontageRewound);
                }

                montageState.LocalMontagePosition = currentPosition;
            }
            else if (montageState.LocalMontage != null)
            {
                WukongMP.Instance.Client.SendMontageCancel(characterState.PeerId);
            }

            montageState.LocalMontage = currentMontage;
            characterState.MontageState = montageState;
        }
    }

    [HarmonyPatch(typeof(EntityManager), nameof(EntityManager.TickAllComponentsWithGroup), typeof(float), typeof(int), typeof(int), typeof(int))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public static class PatchEntityManagerTick
    {
        public static void Postfix(
            int TickGroup, // this is BGW_TickGroupMask
            int ThreadIdx,
            int ThreadCount)
        {
            if (ThreadIdx != 0)
                return;

            var mask = (BGW_TickGroupMask)TickGroup;

            if (mask != BGW_TickGroupMask.TG_PreTick
                && mask != BGW_TickGroupMask.TG_OnTick
                && mask != BGW_TickGroupMask.TG_LateTick
                && mask != BGW_TickGroupMask.TG_ThreadTick)
                return;

            if (mask == BGW_TickGroupMask.TG_OnTick)
            {
                ComponentMonitorManager.Instance.Update();
                WukongMP.Instance.Client.RunTickSystems();
            }

            if (!GameLoopPatch.CustomTickGroupActionQueues.TryGetValue(mask, out var queue))
                return;

            while (queue.TryDequeue(out var item))
            {
                try
                {
                    Logging.LogTrace("Processing {Action} action for tick group {Mask} (EntityManager)", item.Name, mask);
                    item.Action();
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                }
            }
        }
    }
}