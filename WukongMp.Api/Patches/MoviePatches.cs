using System;
using System.Linq;
using System.Reflection;
using b1;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Relay.Common.Mapping;
using UnrealEngine.Engine;
using UnrealEngine.LevelSequence;
using UnrealEngine.MovieScene;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRequestPlayMovie
{
    [HarmonyTargetMethodHint("b1.BGS_MovieSystem", "RequestPlayMovie")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_MovieSystem:RequestPlayMovie");
    }

    public static bool Prefix(GameStateSystemBase __instance, FPlayMovieRequest Request)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        // get methods
        var movieSystemType = __instance.GetType();
        var onPlayMovieInstance = AccessTools.Method(movieSystemType, "OnPlayMovieInstance");

        FMovieSceneSequencePlaybackSettings fMovieSceneSequencePlaybackSettings = new()
        {
            AutoPlay = false,
            PlayRate = 1f,
            StartTime = 0f,
            RandomStartTime = false,
            RestoreState = false,
            DisableMovementInput = true,
            DisableLookAtInput = Request.bDisableLookAtInput,
            HidePlayer = Request.bHidePlayer,
            HideHud = Request.bHideHud,
            DisableCameraCuts = !Request.bDisablePlayerControl,
            PauseAtEnd = false
        };
        FLevelSequenceCameraSettings fLevelSequenceCameraSettings = new()
        {
            AspectRatioAxisConstraint = EAspectRatioAxisConstraint.AspectRatio_MaintainXFOV,
            OverrideAspectRatioAxisConstraint = false
        };
        FMovieGraphPlaySettings fMovieGraphPlaySettings = new()
        {
            PlaybackSettings = fMovieSceneSequencePlaybackSettings,
            CameraSettings = fLevelSequenceCameraSettings,
            bUsePlayerCamera = !Request.bDisablePlayerControl,
            bTriggerMonsterGoHome = false
        };
        MovieInstance movieInstance = MovieInstance.Create(owner, Request.SequenceID, fMovieGraphPlaySettings);
        if (movieInstance == null)
        {
            Request.BeforePlayFinishCallback?.Invoke();
            Request.MovieFinishCallback?.Invoke();
            return false;
        }

        AActor actorByGuid = BGU_DataUtil.GetActorByGuid(owner, Request.OverlapBoxGuid);
        if (actorByGuid != null)
        {
            movieInstance.OverlapGuid = Request.OverlapBoxGuid;
            var componentsByTag = actorByGuid.GetComponentsByTag(UClass.GetClass<USceneComponent>(), B1GlobalFNames.MatchPointA);
            if (componentsByTag.Count > 0)
            {
                movieInstance.PointAPos = ((USceneComponent)componentsByTag[0]).GetWorldTransform();
            }

            componentsByTag = actorByGuid.GetComponentsByTag(UClass.GetClass<USceneComponent>(), B1GlobalFNames.MatchPointB);
            if (componentsByTag.Count > 0)
            {
                movieInstance.PointBPos = ((USceneComponent)componentsByTag[0]).GetWorldTransform();
            }

            movieInstance.MatchingPosType = Request.MatchType;
        }
        else
        {
            movieInstance.OverlapGuid = "";
        }

        if (Request.BeforePlayFinishCallback != null)
        {
            movieInstance.BeforePlayFinishCallBack = (Action)Delegate.Combine(movieInstance.BeforePlayFinishCallBack, Request.BeforePlayFinishCallback);
        }

        if (Request.MovieFinishCallback != null)
        {
            movieInstance.MovieFinishCallBack = (Action)Delegate.Combine(movieInstance.MovieFinishCallBack, Request.MovieFinishCallback);
        }

        SetCallbacks(Request.SequenceID, movieInstance);

        onPlayMovieInstance?.Invoke(__instance, [Request.SequenceID, movieInstance]);

        return false;
    }

    private static void SetCallbacks(int SequenceId, MovieInstance Instance)
    {
        Logging.LogDebug("Playing movie {Name} with sequenceId {Id}", Instance.GetName(), SequenceId);

        if (!Instance.PlaySettings.PlaybackSettings.DisableCameraCuts)
        {
            var playerState = DI.Instance.PlayerState;
            if (playerState.LocalMainCharacter.HasValue)
            {
                ref var localMain = ref playerState.LocalMainCharacter.Value.GetLocalState();
                localMain.IsInSequence = true;
            }

            Logging.LogDebug("Movie with sequenceId {Id} started, hiding all players", SequenceId);
            foreach (var playerId in DI.Instance.State.OtherAreaPlayers)
            {
                var mainEntity = playerState.GetMainCharacterByPlayerId(playerId);
                if (mainEntity == null)
                    continue;
                ref var localMain = ref mainEntity.Value.GetLocalState();
                mainEntity.Value.Pawn?.SetActorHiddenInGame(true);
                localMain.MarkerActor?.SetActorHiddenInGame(true);
                localMain.ShouldDisableCollision = true;
                PlayerUtils.SetCollisionEnabled(mainEntity.Value.Pawn, false);
            }

            Instance.MovieFinishCallBack = (Action)Delegate.Combine(Instance.MovieFinishCallBack, () =>
            {
                var areaEntity = DI.Instance.AreaState.CurrentArea;
                if (areaEntity != null && !areaEntity.Value.GetMovie().FinishedSequences.Contains(SequenceId))
                {
                    DI.Instance.MappedEvent.NotifyEcsIfApplicable(new MovieFinishedEvent(SequenceId, areaEntity.Value.Scope.AreaId), default(EmptyContext));
                }

                var playerState = DI.Instance.PlayerState;
                if (playerState.LocalMainCharacter.HasValue)
                {
                    ref var localMain = ref playerState.LocalMainCharacter.Value.GetLocalState();
                    localMain.IsInSequence = false;
                }

                Logging.LogDebug("Movie with sequenceId {Id} finished, showing all players", SequenceId);
                foreach (var playerId in DI.Instance.State.OtherAreaPlayers)
                {
                    var mainEntity = playerState.GetMainCharacterByPlayerId(playerId);
                    if (!mainEntity.HasValue)
                        continue;
                    ref var localMain = ref mainEntity.Value.GetLocalState();
                    mainEntity.Value.Pawn?.SetActorHiddenInGame(false);
                    localMain.MarkerActor?.SetActorHiddenInGame(false);
                    localMain.ShouldDisableCollision = false;
                    DI.Instance.WidgetManager.HideInfoMessage();
                }
            });
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTickForMovieSystem
{
    [HarmonyTargetMethodHint("b1.BGS_MovieSystem", "TickForMovieSystem")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_MovieSystem:TickForMovieSystem");
    }

    public static bool Prefix(GameStateSystemBase? __instance, float DeltaTime)
    {
        if (__instance == null)
            return true;

        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (DI.Instance.GameplayConfiguration.DisableCutscenes)
            return false;

        var playerState = DI.Instance.PlayerState;

        // get properties
        var movieSystemType = __instance.GetType();
        MethodInfo getter = AccessTools.PropertyGetter(movieSystemType, "MovieData");
        BGC_MovieData MovieData = (BGC_MovieData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(movieSystemType, "GlobalMovieData");
        BIC_MovieData GlobalMovieData = (BIC_MovieData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(movieSystemType, "AnimationSyncData");
        IBGC_AnimationSyncData AnimationSyncData = (IBGC_AnimationSyncData)getter.Invoke(__instance, null);

        // get methods
        MethodInfo RequestPlayMovieMethod = AccessTools.Method(movieSystemType, "RequestPlayMovie");
        MethodInfo OnFinishTransBackMethod = AccessTools.Method(movieSystemType, "OnFinishTransBack");
        MethodInfo TickForDefeatSlowTimeMethod = AccessTools.Method(movieSystemType, "TickForDefeatSlowTime");
        MethodInfo OnSkipCurrentCameraMovieMethod = AccessTools.Method(movieSystemType, "OnSkipCurrentCameraMovie");

        if (!MovieData.bCanTick)
        {
            return false;
        }

        MovieData.bAllSeqCantSkip = AnimationSyncData.IsPlayerInAnimationSyncing(__instance.GetOwner());

        if (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
        {
            var peakRequest = GlobalMovieData.PlayMovieRequestQueue.Peek();
            var mainEntity = playerState.LocalMainCharacter;
            var areaEntity = DI.Instance.AreaState.CurrentArea;
            var isMovieStartedByOthers = areaEntity != null && areaEntity.Value.GetMovie().StartedSequences.Contains(peakRequest.SequenceID);

            if (CutsceneUtils.CheckAllPlayersWaitingForCutscene(DI.Instance.State, DI.Instance.PlayerState, peakRequest.SequenceID) || !peakRequest.bDisablePlayerControl || isMovieStartedByOthers)
            {
                DI.Instance.WidgetManager.HideInfoMessage();
                if (mainEntity != null)
                {
                    ref var localMain = ref mainEntity.Value.GetLocalState();
                    localMain.IsWaitingForSequence = false;
                    localMain.IsJoiningSequence = false;
                }

                while (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
                {
                    var movieRequest = GlobalMovieData.PlayMovieRequestQueue.Dequeue();
                    if (areaEntity != null && !areaEntity.Value.GetMovie().StartedSequences.Contains(movieRequest.SequenceID))
                    {
                        // TODO: Event?
                        DI.Instance.ServerRpc.SendMovieStarted(movieRequest.SequenceID, areaEntity.Value.Scope.AreaId);
                    }

                    MovieData.GetPlayingMovieID(out var playingMovies);
                    playingMovies ??= [];

                    if (playingMovies.Contains(movieRequest.SequenceID))
                        continue;

                    RequestPlayMovieMethod?.Invoke(__instance, [movieRequest]);
                }
            }
            else if (mainEntity?.GetLocalState().IsWaitingForSequence == false)
            {
                // Some cutscenes can be played solo even in multiplayer, e.g. holding on to the Feng-Tail General
                if (Constants.SoloPlaySequences.Contains(peakRequest.SequenceID))
                {
                    return true;
                }

                ref var main = ref mainEntity.Value.GetState();
                ref var localMain = ref mainEntity.Value.GetLocalState();
                DI.Instance.WidgetManager.ShowInfoMessage(Texts.WaitForOtherPlayers);
                main.WaitingSequenceId = peakRequest.SequenceID;
                localMain.IsWaitingForSequence = true;
                localMain.JoiningSequenceLocation = main.Location.ToFVector();
                Logging.LogDebug("Sending waiting for sequence with sequenceId {Id}", peakRequest.SequenceID);
                
                DI.Instance.MappedEvent.NotifyEcsIfApplicable(new WaitingForSequenceEvent(peakRequest.SequenceID, main.Location.ToFVector()), default(EmptyContext));

                // some cutscenes cannot be triggered for multiple players
                // e.g. 3rd act boss attacks one player causing him to enter a cutscene,
                // but other players are stuck since they are not attacked
                if (Constants.InstantTriggerSequences.Contains(peakRequest.SequenceID))
                {
                    DI.Instance.MappedEvent.NotifyEcsIfApplicable(new PlayMovieRequestEvent(peakRequest.SequenceID, peakRequest.bDisablePlayerControl, peakRequest.bDisableMovementInput, peakRequest.bDisableLookAtInput, peakRequest.bHidePlayer, peakRequest.bHideHud, peakRequest.OverlapBoxGuid, peakRequest.MatchType), default(EmptyContext));
                }
            }
        }

        foreach (TStrongObjectPtr<MovieInstance> item in MovieData.MovieInstances.Values.ToList())
        {
            item.Get()?.OnTick(DeltaTime);
        }

        if (MovieData.TransBackTimeForPreviewMovie > 1E-08f)
        {
            MovieData.TransBackTimeForPreviewMovie -= DeltaTime;
            if (MovieData.TransBackTimeForPreviewMovie <= 1E-08f)
            {
                MovieData.TransBackTimeForPreviewMovie = -1f;
                OnFinishTransBackMethod?.Invoke(__instance, null);
            }
        }

        TickForDefeatSlowTimeMethod?.Invoke(__instance, [DeltaTime]);
        if (GSGameplayCVar.CVar_AutoSkipMovies.GetValueInGameThread() != 0 && MovieData.IsCanSkip())
        {
            OnSkipCurrentCameraMovieMethod?.Invoke(__instance, null);
        }

        return false;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnSkipCurrentCameraMovie
{
    [HarmonyTargetMethodHint("b1.BGS_MovieSystem", "OnSkipCurrentCameraMovie")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_MovieSystem:OnSkipCurrentCameraMovie");
    }

    public static bool Prefix(GameStateSystemBase __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (DI.Instance.PlayerState.LocalMainCharacter == null)
            return true;

        var movieSystemType = __instance.GetType();
        MethodInfo getter = AccessTools.PropertyGetter(movieSystemType, "MovieData");
        BGC_MovieData movieData = (BGC_MovieData)getter.Invoke(__instance, null);
        var sequenceId = movieData.CameraMovieInstance?.SequenceId ?? 0;

        var areaEntity = DI.Instance.AreaState.CurrentArea;
        if (areaEntity != null && areaEntity.Value.GetMovie().StartedSequences.Contains(sequenceId) && !areaEntity.Value.GetMovie().FinishedSequences.Contains(sequenceId))
        {
            Logging.LogDebug("Sending skip movie for sequence with sequenceId {Id}", sequenceId);
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new SkipMovieEvent(sequenceId), default(EmptyContext));
            return false;
        }

        Logging.LogDebug("Skipping local movie with sequenceId {Id}", sequenceId);
        return true;
    }
}