using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;

namespace WukongMp.Api.WukongUtils
{
    public static class DebugUtils
    {
        public static UClass? GetDebugCubeActorClass()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                var debugCubeActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.DebugCubeActorPath, ELoadResourceType.SyncLoadAndCache);
                if (debugCubeActorClass == null)
                {
                    Logging.LogError("Cannot find class of {Class} to spawn", Constants.DebugCubeActorPath);
                    return null;
                }
                return debugCubeActorClass;
            }
            return null;
        }

        public static UClass? GetDebugSphereActorClass()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                var debugActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.DebugSphereActorPath, ELoadResourceType.SyncLoadAndCache);
                if (debugActorClass == null)
                {
                    Logging.LogError("Cannot find class of {Class} to spawn", Constants.DebugSphereActorPath);
                    return null;
                }
                return debugActorClass;
            }
            return null;
        }

        public static AActor? SpawnActor(UClass unrealClass, FVector location, FRotator rotation)
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                var actor = world.SpawnActor(unrealClass, ref location, ref rotation);
                if (actor == null)
                {
                    Logging.LogError("Cannot spawn actor {ActorName}", unrealClass.GetName());
                    return null;
                }
                return actor;
            }
            return null;
        }
    }
}
