using b1;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnrealEngine.Engine;
using UnrealEngine.LevelSequence;
using UnrealEngine.MovieScene;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Old;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRequestPlayMovie
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_MovieSystem:RequestPlayMovie");
    }

    public static bool Prefix(GameStateSystemBase __instance, FPlayMovieRequest Request)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        if (UBGWFunctionLibraryCS.HasSequenceAlreadyPlayed(__instance.GetOwner(), Request.SequenceID))
        {
            return false; // Skip the request if the sequence has already been played
        }

        var Owner = __instance.GetOwner();
        // get methods
        var movieSystemType = __instance.GetType();
        MethodInfo OnPlayMovieInstance = AccessTools.Method(movieSystemType, "OnPlayMovieInstance");

        FMovieSceneSequencePlaybackSettings fMovieSceneSequencePlaybackSettings = default(FMovieSceneSequencePlaybackSettings);
        fMovieSceneSequencePlaybackSettings.AutoPlay = false;
        fMovieSceneSequencePlaybackSettings.PlayRate = 1f;
        fMovieSceneSequencePlaybackSettings.StartTime = 0f;
        fMovieSceneSequencePlaybackSettings.RandomStartTime = false;
        fMovieSceneSequencePlaybackSettings.RestoreState = false;
        fMovieSceneSequencePlaybackSettings.DisableMovementInput = true;
        fMovieSceneSequencePlaybackSettings.DisableLookAtInput = Request.bDisableLookAtInput;
        fMovieSceneSequencePlaybackSettings.HidePlayer = Request.bHidePlayer;
        fMovieSceneSequencePlaybackSettings.HideHud = Request.bHideHud;
        fMovieSceneSequencePlaybackSettings.DisableCameraCuts = !Request.bDisablePlayerControl;
        fMovieSceneSequencePlaybackSettings.PauseAtEnd = false;
        FMovieSceneSequencePlaybackSettings playbackSettings = fMovieSceneSequencePlaybackSettings;
        FLevelSequenceCameraSettings fLevelSequenceCameraSettings = default(FLevelSequenceCameraSettings);
        fLevelSequenceCameraSettings.AspectRatioAxisConstraint = EAspectRatioAxisConstraint.AspectRatio_MaintainXFOV;
        fLevelSequenceCameraSettings.OverrideAspectRatioAxisConstraint = false;
        FLevelSequenceCameraSettings cameraSettings = fLevelSequenceCameraSettings;
        FMovieGraphPlaySettings fMovieGraphPlaySettings = default(FMovieGraphPlaySettings);
        fMovieGraphPlaySettings.PlaybackSettings = playbackSettings;
        fMovieGraphPlaySettings.CameraSettings = cameraSettings;
        fMovieGraphPlaySettings.bUsePlayerCamera = !Request.bDisablePlayerControl;
        fMovieGraphPlaySettings.bTriggerMonsterGoHome = false;
        FMovieGraphPlaySettings inPlaySettings = fMovieGraphPlaySettings;
        MovieInstance movieInstance = MovieInstance.Create(Owner, Request.SequenceID, inPlaySettings);
        if (movieInstance == null)
        {
            Request.BeforePlayFinishCallback?.Invoke();
            Request.MovieFinishCallback?.Invoke();
            return false;
        }
        AActor actorByGuid = BGU_DataUtil.GetActorByGuid(Owner, Request.OverlapBoxGuid);
        if (actorByGuid != null)
        {
            movieInstance.OverlapGuid = Request.OverlapBoxGuid;
            List<UActorComponent> componentsByTag = actorByGuid.GetComponentsByTag(UClass.GetClass(typeof(USceneComponent)), B1GlobalFNames.MatchPointA);
            if (componentsByTag.Count > 0)
            {
                movieInstance.PointAPos = ((USceneComponent)componentsByTag[0]).GetWorldTransform();
            }
            componentsByTag = actorByGuid.GetComponentsByTag(UClass.GetClass(typeof(USceneComponent)), B1GlobalFNames.MatchPointB);
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

        OnPlayMovieInstance?.Invoke(__instance, [Request.SequenceID, movieInstance]);

        return false;
    }

    private static void SetCallbacks(int SequenceId, MovieInstance Instance)
    {
        Logging.LogDebug("Playing movie {Name} with sequenceId {Id}", Instance.GetName(), SequenceId);

        if (!Instance.PlaySettings.PlaybackSettings.DisableCameraCuts)
        {
            Logging.LogDebug("Movie with sequenceId {Id} started, hiding all players", SequenceId);
            foreach (var player in DI.Instance.Players.ConnectedPlayers.Values)
            {
                player.Pawn?.SetActorHiddenInGame(true);
                player.MarkerActor?.SetActorHiddenInGame(true);
            }
            Instance.MovieFinishCallBack = (Action)Delegate.Combine(Instance.MovieFinishCallBack, () =>
            {
                Logging.LogDebug("Movie with sequenceId {Id} finished, showing all players", SequenceId);
                foreach (var player in DI.Instance.Players.ConnectedPlayers.Values)
                {
                    player.Pawn?.SetActorHiddenInGame(false);
                    player.MarkerActor?.SetActorHiddenInGame(false);
                }
            });
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTickForMovieSystem
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_MovieSystem:TickForMovieSystem");
    }

    public static bool Prefix(GameStateSystemBase __instance, float DeltaTime)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var players = DI.Instance.Players;

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
        if (AnimationSyncData.IsPlayerInAnimationSyncing(__instance.GetOwner()))
        {
            MovieData.bAllSeqCantSkip = true;
        }
        else
        {
            MovieData.bAllSeqCantSkip = false;
        }
        if (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
        {
            var peakRequest = GlobalMovieData.PlayMovieRequestQueue.Peek();
            if (CutsceneUtils.CheckAllPlayersWaitingForCutscene(peakRequest.SequenceID) || peakRequest.bDisablePlayerControl == false)
            {
                InfoMessageWidget.Instance.SetVisibility(false);
                players.LocalPlayerState.IsWaitingForSequence = false;
                players.LocalPlayerState.IsJoiningSequence = false;
                players.LocalPlayerState.WaitingSequenceId = 0;

                while (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
                {
                    RequestPlayMovieMethod?.Invoke(__instance, [GlobalMovieData.PlayMovieRequestQueue.Dequeue()]);
                    BGW_EventCollection.Get(__instance.GetOwner()).Evt_MarkMoviePlayed(peakRequest.SequenceID);
                }
            }
            else if (!players.LocalPlayerState.IsWaitingForSequence)
            {
                InfoMessageWidget.Instance.SetVisibility(true);
                InfoMessageWidget.Instance.SetText("Wait for other players");
                players.LocalPlayerState.IsWaitingForSequence = true;
                players.LocalPlayerState.SequenceLocation = players.LocalPlayerState.Location;
                players.LocalPlayerState.WaitingSequenceId = peakRequest.SequenceID;
                Logging.LogDebug("Sending waiting for sequence with sequenceId {Id}", peakRequest.SequenceID);
                DI.Instance.Rpc.SendWaitingForSequence(new SequenceWaitingData(peakRequest.SequenceID, players.LocalPlayerState.Location));
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