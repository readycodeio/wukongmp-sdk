using System;
using b1;
using b1.BGW;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS;
using WukongMp.Api.Old.State;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils;

public static class MarkerUtils
{
    public static void CreateMarkerForCharacter(Entity entity)
    {
        GameLoopPatch.QueueOnGameThread(() =>
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

            var teamIdComp = entity.GetComponent<TeamComponent>();
            var nameComp = entity.GetComponent<NicknameComponent>();

            // TODO: Should be created by the archetype, but it is not due to dynamic delta entity creation
            if (!entity.HasComponent<MarkerComponent>())
                entity.AddComponent<MarkerComponent>();

            ref var markerComp = ref entity.GetComponent<MarkerComponent>();

            var teamColor = PvPUtils.GetTeamColorString(teamIdComp.TeamId);
            playerMarkerActor.CallFunctionByNameWithArguments($"SetText {nameComp.Nickname} {teamColor}", true);
            markerComp.MarkerActor = playerMarkerActor;
        }, nameof(CreateMarkerForCharacter));
    }

    [Obsolete]
    public static void CreateMarkerForCharacter(CharacterState characterState)
    {
        GameLoopPatch.QueueOnGameThread(() =>
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

            var teamColor = Constants.IsCoop ? Constants.WhiteTeamColor : PvPUtils.GetTeamColorString(characterState.TeamId);
            playerMarkerActor.CallFunctionByNameWithArguments($"SetText {characterState.NickName} {teamColor}", true);
            characterState.MarkerActor = playerMarkerActor;
        }, nameof(CreateMarkerForCharacter));
    }
}