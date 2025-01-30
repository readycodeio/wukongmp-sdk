using System;
using System.Collections.Concurrent;
using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    public static class GameLoopPatch
    {
        private static readonly ConcurrentDictionary<int, ConcurrentQueue<Action>> CustomTickGroupActionQueues
            = new ConcurrentDictionary<int, ConcurrentQueue<Action>>();

        public static void QueueOnGameThread(Action action, BGW_TickGroupMask tickGroup = BGW_TickGroupMask.TG_OnTick)
        {
            var customTickGroup = TickGroupMaskToCustomTickGroup(tickGroup);
            CustomTickGroupActionQueues.AddOrUpdate(customTickGroup, _ => new ConcurrentQueue<Action>(new[] { action }), (_, queue) =>
            {
                queue.Enqueue(action);
                return queue;
            });
        }

        public static void Prefix(int TickGroup)
        {
            Logging.LogDebug($"Prefix ReceiveTick_Implementation: {TickGroup}");

            if (!CustomTickGroupActionQueues.TryGetValue(TickGroup, out var queue))
                return;

            if (queue.IsEmpty)
                return;

            var enumTickGroup = CustomTickGroupToTickGroupMask(TickGroup);

            Logging.LogDebug($"Processing {queue.Count} action for tick group {enumTickGroup}");

            while (queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Logging.LogError($"-------------- EXCEPTION IN {enumTickGroup} patch -------------");
                    Logging.LogError(e.Message);
                    Logging.LogError(e.StackTrace);
                    Logging.LogError("----------------------------------------------------------------");
                }
            }
        }

        public static void Postfix(int TickGroup)
        {
            Logging.LogDebug($"Postfix ReceiveTick_Implementation: {TickGroup}");
        }

        private static int TickGroupMaskToCustomTickGroup(BGW_TickGroupMask mask)
        {
            switch (mask)
            {
                case BGW_TickGroupMask.TG_AfterAnim:
                    return 2;
                case BGW_TickGroupMask.TG_PostPhysics:
                    return 4;
                case BGW_TickGroupMask.TG_PreAnim:
                    return 101;
                case BGW_TickGroupMask.TG_BeforeStartPhsic:
                    return 111;
                case BGW_TickGroupMask.TG_BeforePostUpdateWork:
                    return 151;
                case BGW_TickGroupMask.TG_OnTick:
                    return 0;
                case BGW_TickGroupMask.TG_PostUpdateWork:
                    return 5;
                case BGW_TickGroupMask.TG_BeforePostPhsic:
                    return 141;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mask), mask, null);
            }
        }

        private static BGW_TickGroupMask CustomTickGroupToTickGroupMask(int tickGroup)
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
                    throw new NotImplementedException("CustomTickGroup_To_BGWTickGroupMask : unknown tickgroup");
            }
        }
    }
}