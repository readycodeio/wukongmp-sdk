using System.Collections.Generic;
using System.Linq;
using b1;
using b1.BGW;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;

namespace WukongMp.Api.WukongUtils
{
    internal static class ModWidgetsUtils
    {
        public static UUserWidget? SpawnWidget(string widgetPath)
        {
            var world = GameUtils.GetWorld();
            if (world == null)
                return null;

            var widgetClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(widgetPath, ELoadResourceType.SyncLoadAndCache);
            if (widgetClass == null)
            {
                Logging.LogError("Cannot find class of {Class} to spawn", widgetPath);
                return null;
            }
            var widget = UGSE_UMGFuncLib.CreateUserWidgetWithClass(world, widgetClass);
            if (widget != null)
            {
                Logging.LogDebug("Widget {Class} spawned successfully", widgetPath);
            }
            else
            {
                Logging.LogError("Cannot spawn widget {Class}", widgetPath);
            }
            return widget;
        }

        private static List<UUserWidget> GetWidgetsByPath(string widgetPath)
        {
            var world = GameUtils.GetWorld();
            if (world == null)
                return [];

            var userWidgets = new List<UUserWidget>();

            var widgetClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(widgetPath, ELoadResourceType.SyncLoadAndCache);
            if (widgetClass == null)
            {
                Logging.LogError("Cannot find class of {Class}", widgetPath);
                return [];
            }

            UWidgetLibrary.GetAllWidgetsOfClass(world, out var list, widgetClass);
            return userWidgets;
        }

        public static UUserWidget? GetWidget(string widgetPath)
        {
            return GetWidgetsByPath(widgetPath).SingleOrDefault();
        }
    }
}
