using System;
using System.Collections.Concurrent;
using System.Threading;
using b1;
using HarmonyLib;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.ECS;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Monitors;
using WukongMp.Api.Old;
using WukongMp.Api.Old.State;
using static Friflo.Engine.ECS.QueryExtensions;

namespace WukongMp.Api.Patches
{
    public static class GameLoopPatch
    {
        public static readonly ConcurrentDictionary<BGW_TickGroupMask, ConcurrentQueue<(Action Action, string? Name)>> CustomTickGroupActionQueues = new();

        /// OBSOLETE: Should be replaced by some ECS command queue
        public static void QueueOnGameThread(Action action, string? name = null, BGW_TickGroupMask tickGroup = BGW_TickGroupMask.TG_OnTick)
        {
            if (tickGroup is BGW_TickGroupMask.TG_LateTick or BGW_TickGroupMask.TG_ThreadTick)
            {
                Logging.LogError("Tick group {Mask} is not supported for queued actions", tickGroup);
                return;
            }

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
                    Logging.LogWarning("CustomTickGroup_To_BGWTickGroupMask : unknown tickgroup");
                    return BGW_TickGroupMask.TG_None;
            }
        }
    }

    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public static class ReceiveTickPatch
    {
        public static void Prefix(ref int TickGroup)
        {
            var mask = GameLoopPatch.CustomTickGroupToTickGroupMask(TickGroup);
            Logging.LogTrace("[{Thread}] Starting tick group {Mask}", Thread.CurrentThread.ManagedThreadId, mask);

            if (mask == BGW_TickGroupMask.TG_None)
            {
                TickGroup = 3;
            }
        }

        public static void Postfix(int TickGroup)
        {
            var mask = GameLoopPatch.CustomTickGroupToTickGroupMask(TickGroup);
            Logging.LogTrace("[{Thread}] Finished tick group {Mask}", Thread.CurrentThread.ManagedThreadId, mask);

            RunQueuedActions(mask);

            if (mask == BGW_TickGroupMask.TG_OnTick)
            {
                RunMontageSync();
                DI.Instance.UpdateLoop.Tick(default);
                ComponentMonitorManager.Instance.Update();
            }
        }

        private static void RunQueuedActions(BGW_TickGroupMask mask)
        {
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

        private static void RunMontageSync()
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            SyncPlayerMontage(DI.Instance.Players.LocalPlayerState);

            DI.Instance.World.Query<LocalTamerComponent, MetadataComponent>().Each(new SyncMontageJob(DI.Instance.Players.LocalPlayerState.PlayerId));
        }

        [Obsolete("To be replaced when we integrate players into ECS")]
        private static void SyncPlayerMontage(CharacterState characterState)
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
                    DI.Instance.Rpc.SendMontageCallback(NetworkIdComponent.FromPlayerId(characterState.PlayerId), currentMontage, currentPosition, hasMontageRewound);
                }

                montageState.LocalMontagePosition = currentPosition;
            }
            else if (montageState.LocalMontage != null)
            {
                DI.Instance.Rpc.SendMontageCancel(NetworkIdComponent.FromPlayerId(characterState.PlayerId));
            }

            montageState.LocalMontage = currentMontage;
            characterState.MontageState = montageState;
        }
    }
}