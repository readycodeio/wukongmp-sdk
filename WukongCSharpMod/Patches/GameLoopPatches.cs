using System;
using System.Collections.Concurrent;
using b1;
using b1.ECS;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    public static class GameLoopPatch
    {
        public static readonly ConcurrentDictionary<BGW_TickGroupMask, ConcurrentQueue<Action>> CustomTickGroupActionQueues
            = new ConcurrentDictionary<BGW_TickGroupMask, ConcurrentQueue<Action>>();

        public static void QueueOnGameThread(Action action, BGW_TickGroupMask tickGroup = BGW_TickGroupMask.TG_OnTick)
        {
            CustomTickGroupActionQueues.AddOrUpdate(tickGroup, _ => new ConcurrentQueue<Action>(new[] { action }), (_, queue) =>
            {
                queue.Enqueue(action);
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
                    throw new NotImplementedException("CustomTickGroup_To_BGWTickGroupMask : unknown tickgroup");
            }
        }
    }

    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    public static class ReceiveTickPatch
    {
        public static void Prefix(int TickGroup)
        {
            var mask = GameLoopPatch.CustomTickGroupToTickGroupMask(TickGroup);

            if (mask == BGW_TickGroupMask.TG_PreTick
                || mask == BGW_TickGroupMask.TG_OnTick
                || mask == BGW_TickGroupMask.TG_LateTick
                || mask == BGW_TickGroupMask.TG_ThreadTick)
                return;

            Logging.LogDebug($"Prefix ReceiveTick_Implementation: {(int)mask} {mask}");

            if (!GameLoopPatch.CustomTickGroupActionQueues.TryGetValue(mask, out var queue))
                return;

            if (queue.IsEmpty)
                return;

            Logging.LogDebug($"Processing {queue.Count} action for tick group {mask}");

            while (queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Logging.LogError($"-------------- EXCEPTION IN {mask} patch -------------");
                    Logging.LogError(e.Message);
                    Logging.LogError(e.StackTrace);
                    Logging.LogError("----------------------------------------------------------------");
                }
            }
        }

        public static void Postfix(int TickGroup)
        {
            var enumTickGroup = GameLoopPatch.CustomTickGroupToTickGroupMask(TickGroup);
            Logging.LogDebug($"Postfix ReceiveTick_Implementation: {(int)enumTickGroup} {enumTickGroup}");
        }
    }

    [HarmonyPatch(typeof(EntityManager), nameof(EntityManager.TickAllComponentsWithGroup), typeof(float), typeof(int), typeof(int), typeof(int))]
    public static class PatchEntityManagerTick
    {
        public static void Prefix(
            int TickGroup, // this is BGW_TickGroupMask
            int ThreadIdx,
            int ThreadCount)
        {
            Logging.LogDebug($"Prefix EntityManager.TickAllComponentsWithGroup: {TickGroup} idx: {ThreadIdx} max: {ThreadCount}");

            if (ThreadIdx != 0)
                return;

            var mask = (BGW_TickGroupMask)TickGroup;

            if (mask != BGW_TickGroupMask.TG_PreTick
                && mask != BGW_TickGroupMask.TG_OnTick
                && mask != BGW_TickGroupMask.TG_LateTick
                && mask != BGW_TickGroupMask.TG_ThreadTick)
                return;

            if (!GameLoopPatch.CustomTickGroupActionQueues.TryGetValue(mask, out var queue))
                return;

            if (queue.IsEmpty)
                return;

            Logging.LogDebug($"Processing {queue.Count} action for tick group {mask}");

            while (queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Logging.LogError($"-------------- EXCEPTION IN {mask} patch (EntityManager) -------------");
                    Logging.LogError(e.Message);
                    Logging.LogError(e.StackTrace);
                    Logging.LogError("-----------------------------------------------------------------------");
                }
            }
        }

        public static void Postfix(
            int TickGroup, // this is BGW_TickGroupMask
            int ThreadIdx,
            int ThreadCount)
        {
            Logging.LogDebug($"Postfix EntityManager.TickAllComponentsWithGroup: {TickGroup} idx: {ThreadIdx} max: {ThreadCount}");
        }
    }
}