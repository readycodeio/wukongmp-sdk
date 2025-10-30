using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils;

public static class MarkerUtils
{
    public static void CreateMarkerForCharacter(TamerEntity tamerEntity)
    {
        var markerActor = SpawnMarkerActor();
        if (markerActor == null)
            return;

        ref readonly var teamComp = ref tamerEntity.GetTeam();
        ref var nameComp = ref tamerEntity.GetNickname();
        ref var markerComp = ref tamerEntity.GetMarker();

        var teamColor = PvPUtils.GetTeamColorString(teamComp.TeamId);
        markerActor.CallFunctionByNameWithArguments($"SetText {nameComp.Nickname} {teamColor}", true);
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

    public static AActor? CreateMarkerForCharacter(MainCharacterEntity mainEntity)
    {
        var markerActor = SpawnMarkerActor();
        if (markerActor == null)
            return null;

        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref readonly var teamComp = ref mainEntity.GetTeam();

        var teamColor = Constants.IsCoop ? Constants.WhiteTeamColor : PvPUtils.GetTeamColorString(teamComp.TeamId);
        markerActor.CallFunctionByNameWithArguments($"SetText {mainComp.CharacterNickName} {teamColor}", true);
        localMainComp.MarkerActor = markerActor;

        return markerActor;
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