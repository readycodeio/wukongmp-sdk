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
        private static readonly object _lockObj = new object();
        private static bool _isExecuting = false;

        public static string Name => typeof(GameUtils).Namespace;

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
            //var widgetAsset = UEditorAssetLibrary.LoadAsset("'/Game/Mods/CustomLuaMod/WBP_MultiplayerChat.WBP_MultiplayerChat_C");
            //if (widgetAsset != null)
            //{
            var wiClass = new TSubclassOf<UUserWidget>();
            wiClass.SetClass<UUserWidget>();
            UWidgetLibrary.GetAllWidgetsOfClass(world, out list, wiClass, false);
            foreach (var widget in list)
            {
                Console.WriteLine(widget.GetType());
            }
            //}
            //else
            //{
            //    Console.WriteLine("Could not load asset");
            //}

            return list;
        }
    }
}