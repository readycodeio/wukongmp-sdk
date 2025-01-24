using System;
using System.Collections.Generic;
using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;

namespace WukongCSharpMod
{
    public static class GameUtils
    {
        private static UWorld _world;

        public static UWorld GetWorld()
        {
            if (_world == null)
            {
                var obj = GCHelper.FindRef(FGlobals.GWorld)?.Managed;
                _world = (UWorld)(obj is UWorld ? obj : null);
            }

            return _world;
        }

        public static APawn GetControlledPawn()
        {
            return UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld()).GetControlledPawn();
        }

        public static BGUPlayerCharacterCS GetBguPlayerCharacterCs()
        {
            var controlledPawn = GetControlledPawn();
            return (BGUPlayerCharacterCS)(controlledPawn is BGUPlayerCharacterCS ? controlledPawn : null);
        }

        public static IEnumerable<BGUCharacterCS> GetMonsters()
        {
            var world = GetWorld();
            if (world == null)
                yield break;

            var actors = world.GetAllActorsOfClass<BUTamerActor>();
            foreach (var actor in actors)
            {
                Console.WriteLine($"Found actor: {actor.GetName()}");

                var monster = actor.GetMonster();
                if (monster != null)
                {
                    Console.WriteLine("Actor is a monster");
                    yield return monster;
                }
            }
        }

        public static BGP_PlayerControllerB1 GetPlayerController()
        {
            return (BGP_PlayerControllerB1)UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld());
        }

        public static BUS_GSEventCollection GetBUS_GSEventCollection()
        {
            return BUS_EventCollectionCS.Get(GetControlledPawn());
        }

        public static BGUPlayerCharacterCS GetThis()
        {
            var controlledPawn = GetControlledPawn();
            return (BGUPlayerCharacterCS)(controlledPawn is BGUPlayerCharacterCS ? controlledPawn : null);
        }

        public static List<UUserWidget> GetWidgets()
        {
            var world = GetWorld();
            if (world == null)
                return null;

            var list = new List<UUserWidget>();
            var userWidgets = new List<UUserWidget>();

            var wiClass = new TSubclassOf<UUserWidget>();
            wiClass.SetClass<UUserWidget>();
            UWidgetLibrary.GetAllWidgetsOfClass(world, out list, wiClass, true);
            foreach (var widget in list)
            {
                Console.WriteLine(widget.GetType());
                Console.WriteLine(widget.GetFullName());
                if (widget.GetFullName().Contains("WBP_MultiplayerChat_C"))
                {
                    userWidgets.Add(widget);
                }
            }

            return userWidgets;
        }

        public static string GetSaveDirectory()
        {
            return FPaths.Combine(FPaths.ProjectDir, "Saved", "Readycode");
        }

        public static string GetSaveFileFullName(string SlotName)
        {
            SlotName += ".sav";
            return FPaths.Combine(GetSaveDirectory(), SlotName);
        }
    }
}