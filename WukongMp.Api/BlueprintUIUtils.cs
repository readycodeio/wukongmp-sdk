using System.Collections.Generic;
using System.Linq;
using b1;
using b1.BGW;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;

namespace WukongMp.Api
{
    public static class BlueprintUiUtils
    {
        public static void SpawnUiManagerActor()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                var uiManagerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.UiManagerActorPath, ELoadResourceType.SyncLoadAndCache);
                if (uiManagerActorClass == null)
                {
                    Logging.LogError("Cannot find class of {Class} to spawn", Constants.UiManagerActorPath);
                    return;
                }
                var uiManagerActor = BGU_UnrealWorldUtil.SpawnActor(world, uiManagerActorClass);
                if (uiManagerActor != null)
                {
                    Logging.LogInformation("UI Manager actor spawned successfully");
                }
                else
                {
                    Logging.LogError("Cannot spawn UI Manager actor");
                }
            }
        }

        private static List<UUserWidget> GetWidgetsByName(string widgetName)
        {
            var world = GameUtils.GetWorld();
            if (world == null)
                return [];

            var userWidgets = new List<UUserWidget>();

            var wiClass = new TSubclassOf<UUserWidget>();
            wiClass.SetClass<UUserWidget>();
            UWidgetLibrary.GetAllWidgetsOfClass(world, out var list, wiClass);
            foreach (var widget in list)
            {
                if (widget.GetFullName().Contains(widgetName))
                {
                    userWidgets.Add(widget);
                }
            }

            return userWidgets;
        }

        public static UUserWidget? GetWidget(string widgetName)
        {
            return GetWidgetsByName(widgetName).SingleOrDefault();
        }
    }
}