using b1;
using b1.BGW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.UI;

namespace WukongMp.Api.WukongUtils
{
    public static class ModWidgetsUtils
    {
        public static void SpawnWidgetManagerActor()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                var widgetManagerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.WidgetManagerActorPath, ELoadResourceType.SyncLoadAndCache);
                if (widgetManagerActorClass == null)
                {
                    Logging.LogError("Cannot find class of {Class} to spawn", Constants.WidgetManagerActorPath);
                    return;
                }
                var widgetManagerActor = BGU_UnrealWorldUtil.SpawnActor(world, widgetManagerActorClass);
                if (widgetManagerActor != null)
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

        public static void InitializeWidgets()
        {
            ChatWidget.Instance.Initialize();
            ChatWidget.Instance.SetVisibility(false);
            TimerWidget.Instance.Initialize();
            LobbyStatusWidget.Instance.Initialize();
            CoopStatusWidget.Instance.Initialize();
            GameMessageWidget.Instance.Initialize();
            CountdownWidget.Instance.Initialize();
            InfoMessageWidget.Instance.Initialize();
            PingIndicatorWidget.Instance.Initialize();
            PingIndicatorWidget.Instance.SetVisibility(true);
            FreeCameraControlsWidget.Instance.Initialize();
        }

        public static void DeinitializeWidgets()
        {
            ChatWidget.Instance.Deinitialize();
            TimerWidget.Instance.Deinitialize();
            LobbyStatusWidget.Instance.Deinitialize();
            CoopStatusWidget.Instance.Deinitialize();
            GameMessageWidget.Instance.Deinitialize();
            CountdownWidget.Instance.Deinitialize();
            InfoMessageWidget.Instance.Deinitialize();
            PingIndicatorWidget.Instance.Deinitialize();
            FreeCameraControlsWidget.Instance.Deinitialize();
        }
    }
}
