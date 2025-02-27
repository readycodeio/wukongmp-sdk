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
        public static void SpawnUIManagerActor()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                var UIManagerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.UiManagerActorPath, ELoadResourceType.SyncLoadAndCache);
                var UIManagerActor = BGU_UnrealWorldUtil.SpawnActor(world, UIManagerActorClass);
                if (UIManagerActor != null)
                {
                    Logging.LogDebug("UI Manager actor spawned successfully");
                }
                else
                {
                    Logging.LogDebug("Cannot spawn UI Manager actor");
                }
            }
        }

        private static List<UUserWidget> GetWidgetsByName(string widgetName)
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
                if (widget.GetFullName().Contains(widgetName))
                {
                    userWidgets.Add(widget);
                }
            }

            return userWidgets;
        }

        public static UUserWidget GetWidget(string widgetName)
        {
            var widgets = GetWidgetsByName(widgetName);
            if (widgets != null && widgets.Count == 1)
            {
                return widgets[0];
            }

            return null;
        }
    }
}