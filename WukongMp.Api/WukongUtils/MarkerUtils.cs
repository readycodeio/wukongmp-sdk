using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils;

internal static class MarkerUtils
{
    public static void CreateMarkerForCharacter(TamerEntity tamerEntity, string color)
    {
        var markerActor = SpawnMarkerActor();
        if (markerActor == null)
            return;

        ref var nameComp = ref tamerEntity.GetNickname();
        ref var markerComp = ref tamerEntity.GetMarker();

        markerActor.CallFunctionByNameWithArguments($"SetText {nameComp.Nickname} {color}", true);
        markerComp.MarkerActor = markerActor;
        markerComp.DestroyQueued = false;
    }

    public static void DestroyMarkerForCharacter(TamerEntity tamerEntity)
    {
        ref var markerComp = ref tamerEntity.GetMarker();

        if (!markerComp.DestroyQueued)
        {
            Logging.LogDebug("Destroying marker for monster {NetId}, guid {Guid}", tamerEntity.GetMeta().NetId, tamerEntity.GetTamer().Guid);
            markerComp.DestroyQueued = true;

            var markerActor = markerComp.MarkerActor;
            if (!markerActor.IsNullOrDestroyed())
            {
                BGU_UnrealWorldUtil.DestroyActor(markerActor);
            }

            markerComp.MarkerActor = null;
        }
    }

    public static AActor? CreateMarkerForCharacter(MainCharacterEntity mainEntity, string color)
    {
        var markerActor = SpawnMarkerActor();
        if (markerActor == null)
            return null;

        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();

        markerActor.CallFunctionByNameWithArguments($"SetText {mainComp.CharacterNickName} {color}", true);
        localMainComp.MarkerActor = markerActor;

        return markerActor;
    }

    public static AActor? SpawnMarkerActor()
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