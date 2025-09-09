using b1;
using b1.BGW;
using System.Collections.Generic;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.WukongUtils;

public static class DebugUtils
{
    private static List<AActor> _tmpActors = [];

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

    public static List<AActor> GetInvisibleWallsAroundPlayer(float radius)
    {
        var world = GameUtils.GetWorld();
        var allActors = UGameplayStatics.GetAllActorsOfClass<AActor>(world);
        var playerLocation = GameUtils.GetControlledPawn()?.GetActorLocation() ?? FVector.ZeroVector;
        var wallActors = new List<AActor>();

        foreach (var actor in allActors)
        {
            var className = actor.GetClass().GetName();
            var distance = FVector.Distance(actor.GetActorLocation(), playerLocation);
            if (distance < radius && className.Contains("BP_DynamicObstcle"))
            {
                wallActors.Add(actor);
            }
        }
        return wallActors;
    }

    public static void AddMarkerToActors(List<AActor> actors)
    {
        var world = GameUtils.GetWorld();
        var playerMarkerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);

        int i = 0;
        foreach (var actor in actors)
        {
            var guid = BGU_DataUtil.GetActorGuid(actor);
            Logging.LogDebug("{Id}: Processing actor with class: {ActorClass}, name: {ActorName}, guid {ActorGuid}", i++, actor.GetClass().GetName(), actor.GetName(), guid);
            var playerMarkerActor = BGU_UnrealWorldUtil.SpawnActor(world, playerMarkerActorClass);
            if (playerMarkerActor == null)
            {
                Logging.LogError("Cannot spawn player marker actor");
                return;
            }
            playerMarkerActor.CallFunctionByNameWithArguments($"SetText {guid} ()", true);
            playerMarkerActor.SetActorLocation(actor.GetActorLocation(), false, out _, true);
            _tmpActors.Add(playerMarkerActor);
        }
    }

    public static void DestroyTmpMarkerActors()
    {
        foreach (var actor in _tmpActors)
        {
            actor.DestroyActor();
        }
        _tmpActors.Clear();
    }

    public static void ShowMarkersForInvisibleWalls(float radius)
    {
        AddMarkerToActors(GetInvisibleWallsAroundPlayer(radius));
    }
}