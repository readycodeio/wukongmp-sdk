using System;
using System.Collections.Concurrent;
using b1;
using b1.ECS;
using HarmonyLib;

namespace WukongApi.Patches
{
    public static class GameLoopPatch
    {
        public static readonly ConcurrentDictionary<BGW_TickGroupMask, ConcurrentQueue<(Action Action, string Name)>> CustomTickGroupActionQueues
            = new ConcurrentDictionary<BGW_TickGroupMask, ConcurrentQueue<(Action, string)>>();

        public static void QueueOnGameThread(Action action, string name = null, BGW_TickGroupMask tickGroup = BGW_TickGroupMask.TG_OnTick)
        {
            if (name != null)
            {
                Logging.LogDebug($"Enqueueing action: {name}");
            }

            CustomTickGroupActionQueues.AddOrUpdate(tickGroup, _ => new ConcurrentQueue<(Action, string)>(new[] { (action, name) }), (_, queue) =>
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
                    throw new NotImplementedException("CustomTickGroup_To_BGWTickGroupMask : unknown tickgroup");
            }
        }
    }

    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class ReceiveTickPatch
    {
        public static void Postfix(int TickGroup)
        {
            var mask = GameLoopPatch.CustomTickGroupToTickGroupMask(TickGroup);

            if (mask == BGW_TickGroupMask.TG_PreTick
                || mask == BGW_TickGroupMask.TG_OnTick
                || mask == BGW_TickGroupMask.TG_LateTick
                || mask == BGW_TickGroupMask.TG_ThreadTick)
                return;

            if (!GameLoopPatch.CustomTickGroupActionQueues.TryGetValue(mask, out var queue))
                return;

            if (queue.IsEmpty)
                return;

            while (queue.TryDequeue(out var item))
            {
                try
                {
                    if (item.Name != null)
                    {
                        Logging.LogDebug($"Processing {item.Name} action for tick group {mask}");
                    }

                    item.Action();
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
    }

    [HarmonyPatch(typeof(EntityManager), nameof(EntityManager.TickAllComponentsWithGroup), typeof(float), typeof(int), typeof(int), typeof(int))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
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

            if (!GameLoopPatch.CustomTickGroupActionQueues.TryGetValue(mask, out var queue))
                return;

            if (queue.IsEmpty)
                return;

            while (queue.TryDequeue(out var item))
            {
                try
                {
                    if (item.Name != null)
                    {
                        Logging.LogDebug($"Processing {item.Name} action for tick group {mask} (EntityManager)");
                    }

                    item.Action();
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
    }
}