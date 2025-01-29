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
            if (TickGroup == 0) // 1024
            {
                // main tick
                while (MainThreadActionQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Logging.LogError("-------------- EXCEPTION ON GAME THREAD --------------");
                        Logging.LogError(e.Message);
                        Logging.LogError(e.StackTrace);
                        Logging.LogError("------------------------------------------------------");
                    }
                }
            }
            else if (TickGroup == 2) // 8
            {
                // after anim
                while (AnimThreadActionQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Logging.LogError("-------------- EXCEPTION ON GAME THREAD --------------");
                        Logging.LogError(e.Message);
                        Logging.LogError(e.StackTrace);
                        Logging.LogError("------------------------------------------------------");
                    }
                }
            }
        }
    }
}