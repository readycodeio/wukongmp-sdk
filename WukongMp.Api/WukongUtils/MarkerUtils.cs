using System;
using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.WukongUtils;

public static class MarkerUtils
{
    public static void CreateMarkerForCharacter(TamerEntity tamerEntity)
    {
        var world = GameUtils.GetWorld();
        var playerMarkerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);
        var playerMarkerActor = BGU_UnrealWorldUtil.SpawnActor(world, playerMarkerActorClass);
        if (playerMarkerActor != null)
        {
            Logging.LogDebug("Player marker actor spawned successfully");
        }
        else
        {
            Logging.LogError("Cannot spawn player marker actor");
            return;
        }

        ref readonly var teamComp = ref tamerEntity.GetTeam();
        ref var nameComp = ref tamerEntity.GetNickname();

        // TODO: Should be created by the archetype, but it is not due to dynamic delta entity creation
        if (!tamerEntity.HasMarker())
            tamerEntity.AddMarker();

        ref var markerComp = ref tamerEntity.GetMarker();

        var teamColor = PvPUtils.GetTeamColorString(teamComp.TeamId);
        playerMarkerActor.CallFunctionByNameWithArguments($"SetText {nameComp.Nickname} {teamColor}", true);
        markerComp.MarkerActor = playerMarkerActor;
    }

    [Obsolete]
    public static AActor? CreateMarkerForCharacter(MainCharacterEntity mainEntity)
    {
        var world = GameUtils.GetWorld();
        var playerMarkerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);
        var playerMarkerActor = BGU_UnrealWorldUtil.SpawnActor(world, playerMarkerActorClass);

        if (playerMarkerActor == null)
        {
            Logging.LogError("Cannot spawn player marker actor");
            return null;
        }

        Logging.LogDebug("Player marker actor spawned successfully");

        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref readonly var teamComp = ref mainEntity.GetTeam();

        var teamColor = Constants.IsCoop ? Constants.WhiteTeamColor : PvPUtils.GetTeamColorString(teamComp.TeamId);
        playerMarkerActor.CallFunctionByNameWithArguments($"SetText {mainComp.CharacterNickName} {teamColor}", true);
        localMainComp.MarkerActor = playerMarkerActor;

        return playerMarkerActor;
    }
}