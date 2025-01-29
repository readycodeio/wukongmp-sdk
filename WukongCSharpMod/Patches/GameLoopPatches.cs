using System;
using System.Collections.Concurrent;
using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    public static class GameLoopPatch
    {
        private static readonly ConcurrentQueue<Action> MainThreadActionQueue = new ConcurrentQueue<Action>();
        private static readonly ConcurrentQueue<Action> AnimThreadActionQueue = new ConcurrentQueue<Action>();

        public static void QueueOnGameThread(Action action)
        {
            MainThreadActionQueue.Enqueue(action);
        }

        public static void QueueOnAnimThread(Action action)
        {
            AnimThreadActionQueue.Enqueue(action);
        }

        public static void Prefix(float DeltaSeconds, int TickGroup)
        {
            Logging.LogDebug($"Prefix ReceiveTick_Implementation: {TickGroup}");

            if (TickGroup == 101) // 1024
            {
                // main tick
                Logging.LogDebug("Main tick dequeue");
                while (MainThreadActionQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Logging.LogError("-------------- EXCEPTION PRE BEFORE ANIM -------------");
                        Logging.LogError(e.Message);
                        Logging.LogError(e.StackTrace);
                        Logging.LogError("------------------------------------------------------");
                    }
                }
            }
            else if (TickGroup == 2) // 8
            {
                // after anim
                Logging.LogDebug("After Anim tick dequeue");
                while (AnimThreadActionQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Logging.LogError("------------- EXCEPTION POST AFTER ANIM -------------");
                        Logging.LogError(e.Message);
                        Logging.LogError(e.StackTrace);
                        Logging.LogError("-----------------------------------------------------");
                    }
                }
            }
        }

        public static void Postfix(float DeltaSeconds, int TickGroup)
        {
            Logging.LogDebug($"Postfix ReceiveTick_Implementation: {TickGroup}");
        }
    }
}