using System;
using b1.BGW;
using ReadyM.Api.Command;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Command;

public class ObstacleCommandRegistration : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("colliders", ConsoleCommand.Create(ToggleDynamicObstacles, isDebugOnly: true));
    }

    private void ToggleDynamicObstacles()
    {
        try
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                UClass dynamicObstacleClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>("Blueprint'/Game/00Main/BPLibrary/SceneObj/BP_DynamicObstcle.BP_DynamicObstcle_C'", ELoadResourceType.SyncLoadAndCache);
                DebugUtils.ToggleBoxTemp(dynamicObstacleClass, world);
            }
        }
        catch (Exception e)
        {
            USharpExceptionHandler.HandleException(e, EUSharpExceptionType.NativeReflectionInvokeFunction);
        }
    }
}