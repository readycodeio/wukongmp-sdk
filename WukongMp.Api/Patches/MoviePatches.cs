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
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (UBGWFunctionLibraryCS.HasSequenceAlreadyPlayed(__instance.GetOwner(), Request.SequenceID))
        {
            return false; // Skip the request if the sequence has already been played
        }

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

        onPlayMovieInstance?.Invoke(__instance, [Request.SequenceID, movieInstance]);

        return false;
    }

    private static void SetCallbacks(int SequenceId, MovieInstance Instance)
    {
        Logging.LogDebug("Playing movie {Name} with sequenceId {Id}", Instance.GetName(), SequenceId);

        if (!Instance.PlaySettings.PlaybackSettings.DisableCameraCuts)
        {
            Logging.LogDebug("Movie with sequenceId {Id} started, hiding all players", SequenceId);
            foreach (var playerId in DI.Instance.State.OtherAreaPlayers)
            {
                var mainEntity = DI.Instance.PlayerState.GetMainCharacterById(playerId);
                if (mainEntity == null)
                    continue;
                ref var localMain = ref mainEntity.Value.GetLocalState();
                localMain.Pawn?.SetActorHiddenInGame(true);
                localMain.MarkerActor?.SetActorHiddenInGame(true);
            }

            Instance.MovieFinishCallBack = (Action)Delegate.Combine(Instance.MovieFinishCallBack, () =>
            {
                Logging.LogDebug("Movie with sequenceId {Id} finished, showing all players", SequenceId);
                foreach (var playerId in DI.Instance.State.OtherAreaPlayers)
                {
                    var mainEntity = DI.Instance.PlayerState.GetMainCharacterById(playerId);
                    if (!mainEntity.HasValue)
                        continue;
                    ref var localMain = ref mainEntity.Value.GetLocalState();
                    localMain.Pawn?.SetActorHiddenInGame(false);
                    localMain.MarkerActor?.SetActorHiddenInGame(false);
                    InfoMessageWidget.Instance.SetVisibility(false);
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
        if (!DI.Instance.AreaState.InRoom)
            return true;

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
            var mainEntity = playerState.LocalMainCharacter;

            if (CutsceneUtils.CheckAllPlayersWaitingForCutscene(peakRequest.SequenceID) || peakRequest.bDisablePlayerControl == false)
            {
                InfoMessageWidget.Instance.SetVisibility(false);
                if (mainEntity != null)
                {
                    ref var localMain = ref mainEntity.Value.GetLocalState();
                    localMain.IsWaitingForSequence = false;
                    localMain.IsJoiningSequence = false;
                    localMain.LastSyncableSequenceId = peakRequest.SequenceID;
                }

                while (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
                {
                    RequestPlayMovieMethod?.Invoke(__instance, [GlobalMovieData.PlayMovieRequestQueue.Dequeue()]);
                    BGW_EventCollection.Get(__instance.GetOwner()).Evt_MarkMoviePlayed(peakRequest.SequenceID);
                }
            }
            else if (mainEntity?.GetLocalState().IsWaitingForSequence == false)
            {
                ref var main = ref mainEntity.Value.GetState();
                ref var localMain = ref mainEntity.Value.GetLocalState();
                InfoMessageWidget.Instance.SetVisibility(true);
                InfoMessageWidget.Instance.SetText("Wait for other players");
                main.WaitingSequenceId = peakRequest.SequenceID;
                localMain.IsWaitingForSequence = true;
                localMain.JoiningSequenceLocation = main.Location.ToFVector();
                Logging.LogDebug("Sending waiting for sequence with sequenceId {Id}", peakRequest.SequenceID);
                DI.Instance.Rpc.SendWaitingForSequence(new SequenceWaitingData(peakRequest.SequenceID, main.Location.ToFVector()));
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

        if (DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().LastSyncableSequenceId == sequenceId)
        {
            Logging.LogDebug("Sending skip movie for sequence with sequenceId {Id}", sequenceId);
            InfoMessageWidget.Instance.SetVisibility(true);
            InfoMessageWidget.Instance.SetText("Wait for other players");
            DI.Instance.ServerRpc.SendSkipMovie(sequenceId);
            return false;
        }

        Logging.LogDebug("Skipping local movie with sequenceId {Id}", sequenceId);
        return true;
    }
}