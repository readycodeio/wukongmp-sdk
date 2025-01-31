using System;
using System.Collections.Concurrent;
using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
    public static class GameLoopPatch
    {
        private struct Entry
        {
            public readonly ConcurrentQueue<Action> threadActionQueue;
            public readonly ConcurrentQueue<Action> loopActionQueue;

            private Entry(ConcurrentQueue<Action> threadActionQueue, ConcurrentQueue<Action> loopActionQueue)
            {
                this.threadActionQueue = threadActionQueue;
                this.loopActionQueue = loopActionQueue;
            }
            
            public static Entry Create()
                => new Entry(
                    new ConcurrentQueue<Action>(), 
                    new ConcurrentQueue<Action>()
                );
        }
        
        private static readonly Entry mainThreadEntry = Entry.Create();
        private static readonly Entry animThreadEntry = Entry.Create();

        public static void LoopOnGameThread(Action action)
        {
            mainThreadEntry.loopActionQueue.Enqueue(action);
        }

        public static void LoopOnAnimThread(Action action)
        {
            animThreadEntry.loopActionQueue.Enqueue(action);
        }

        public static void QueueOnGameThread(Action action)
        {
            mainThreadEntry.threadActionQueue.Enqueue(action);
        }

        public static void QueueOnAnimThread(Action action)
        {
            animThreadEntry.threadActionQueue.Enqueue(action);
        }

        private static void RunEntry(Entry entry, string name)
        {
            if (entry.threadActionQueue.Count > 0)
            {
                Logging.LogDebug($"'{name}' Tick Dequeue");
                while (entry.threadActionQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Logging.LogError($"-------------- EXCEPTION DEQUEUE '{name}' -------------");
                        Logging.LogError(e.Message);
                        Logging.LogError(e.StackTrace);
                        Logging.LogError("------------------------------------------------------");
                    }
                }
            }
            
            foreach (var action in entry.loopActionQueue)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Logging.LogError($"-------------- EXCEPTION LOOP '{name}' -------------");
                    Logging.LogError(e.Message);
                    Logging.LogError(e.StackTrace);
                    Logging.LogError("------------------------------------------------------");
                }
            }
        }
        
        public static void Prefix(float DeltaSeconds, int TickGroup)
        {
            // Logging.LogDebug($"Prefix ReceiveTick_Implementation: {TickGroup}");
            
            if (TickGroup == 101) // 1024
            {
                // main tick
                RunEntry(mainThreadEntry, "Main");
            }
            else if (TickGroup == 2) // 8
            {
                // after anim
                RunEntry(animThreadEntry, "Anim");
            }
        }

        public static void Postfix(float DeltaSeconds, int TickGroup)
        {
            // Logging.LogDebug($"Postfix ReceiveTick_Implementation: {TickGroup}");
        }
    }
}