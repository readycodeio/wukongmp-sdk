using b1;
using b1.BGW;
using CSharpModBase;
using Friflo.Engine.ECS;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils;

internal static class MarkerUtils
{
    public static void CreateMarkerForPlayer(Entity entity, string text, string color)
    {
        if (!entity.HasComponent<MarkerComponent>())
        {
            Logging.LogError("Entity {Entity} does not have MarkerComponent", entity.GetNetId());
            return;
        }

        ref var localMainComp = ref entity.GetComponent<MarkerComponent>();

        var markerActor = localMainComp.MarkerActor ?? SpawnMarkerActor();
        if (markerActor == null)
        {
            Logging.LogError("Failed to create marker actor for player {Entity}", entity.GetNetId());
            return;
        }

        markerActor.CallFunctionByNameWithArguments($"SetText {text} {color}", true);
        localMainComp.MarkerActor = markerActor;
    }

    public static void DestroyMarkerForCharacter(Entity entity)
    {
        if (!entity.HasComponent<MarkerComponent>())
            return;

        ref var markerComp = ref entity.GetComponent<MarkerComponent>();

        if (!markerComp.DestroyQueued)
        {
            Logging.LogDebug("Destroying marker for entity {NetId}", entity.GetNetId());
            markerComp.DestroyQueued = true;

            var markerActor = markerComp.MarkerActor;
            if (!markerActor.IsNullOrDestroyed())
            {
                Utils.TryRunOnGameThread(() => { BGU_UnrealWorldUtil.DestroyActor(markerActor); });
            }

            markerComp.MarkerActor = null;
        }
    }

    private static AActor? SpawnMarkerActor()
    {
        var world = GameUtils.GetWorld();
        var markerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);
        if (markerActorClass == null)
        {
            Logging.LogError("Cannot load marker class");
            return null;
        }

        var markerActor = BGU_UnrealWorldUtil.SpawnActor(world, markerActorClass);
        if (markerActor == null)
        {
            Logging.LogError("Cannot spawn marker actor");
            return null;
        }

        Logging.LogDebug("Marker actor spawned successfully");
        return markerActor;
    }
}