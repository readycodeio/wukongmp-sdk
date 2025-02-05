using System;
using System.Collections.Generic;
using b1;
using b1.BGW;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;

namespace WukongApi
{
    public static class BlueprintUIUtils
    {
        public static void SpawnModActor()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                var modActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.ModActorPath, ELoadResourceType.SyncLoadAndCache);
                var modActor = BGU_UnrealWorldUtil.SpawnActor(world, modActorClass);
                if (modActor != null)
                {
                    Logging.LogDebug("ModActor spawned successfully");
                }
                else
                {
                    Logging.LogDebug("Cannot spawn ModActor");
                }
            }
        }

        private static List<UUserWidget> GetChatWidgets()
        {
            var world = GameUtils.GetWorld();
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
                if (widget.GetFullName().Contains(Constants.ChatWidgetName))
                {
                    userWidgets.Add(widget);
                }
            }

            return userWidgets;
        }

        public static UUserWidget GetChatWidget()
        {
            var widgets = GetChatWidgets();
            if (widgets != null && widgets.Count == 1)
            {
                return widgets[0];
            }

            return null;
        }
    }
}