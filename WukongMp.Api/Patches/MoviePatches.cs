using b1;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Old;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnPlayMovieInstance
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_MovieSystem:OnPlayMovieInstance");
    }

    public static void Prefix(int SequenceId, MovieInstance Instance)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        Logging.LogDebug("Playing movie {Name} with sequenceId {Id}", Instance.GetName(), SequenceId);

        if (!Instance.PlaySettings.PlaybackSettings.DisableCameraCuts)
        {
            Logging.LogDebug("Movie with sequenceId {Id} started, hiding all players", SequenceId);
            foreach (var player in WukongMpModBase.Client.ConnectedPlayers.Values)
            {
                player.Pawn?.SetActorHiddenInGame(true);
            }
            Instance.MovieFinishCallBack = (Action)Delegate.Combine(Instance.MovieFinishCallBack, () =>
            {
                Logging.LogDebug("Movie with sequenceId {Id} finished, showing all players", SequenceId);
                foreach (var player in WukongMpModBase.Client.ConnectedPlayers.Values)
                {
                    player.Pawn?.SetActorHiddenInGame(false);
                }
            });
        }
    }
}

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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        if (UBGWFunctionLibraryCS.HasSequenceAlreadyPlayed(__instance.GetOwner(), Request.SequenceID))
        {
            return false; // Skip the request if the sequence has already been played
        }
        return true;
    }

    public static void Postfix(GameStateSystemBase __instance, FPlayMovieRequest Request)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        Logging.LogDebug("RequestPlayMovie called with sequenceId {Id}, bDisablePlayerControl {Control}, bDisableMovementInput {Movement}, bDisableLookAtInput {LookAt}, bHidePlayer {HidePlayer}, bHideHud {HideHud}, MatchType {MatchType}",
            Request.SequenceID, Request.bDisablePlayerControl, Request.bDisableMovementInput, Request.bDisableLookAtInput, Request.bHidePlayer, Request.bHideHud, Request.MatchType);
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMpMod.Client;

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
                client.LocalPlayerState.IsWaitingForSequence = false;
                client.LocalPlayerState.IsJoiningSequence = false;
                client.LocalPlayerState.WaitingSequenceId = 0;

                while (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
                {
                    RequestPlayMovieMethod?.Invoke(__instance, [GlobalMovieData.PlayMovieRequestQueue.Dequeue()]);
                    BGW_EventCollection.Get(__instance.GetOwner()).Evt_MarkMoviePlayed(peakRequest.SequenceID);
                }
            }
            else if (!client.LocalPlayerState.IsWaitingForSequence)
            {
                InfoMessageWidget.Instance.SetVisibility(true);
                InfoMessageWidget.Instance.SetText("Wait for other players");
                client.LocalPlayerState.IsWaitingForSequence = true;
                client.LocalPlayerState.SequenceLocation = client.LocalPlayerState.Location;
                client.LocalPlayerState.WaitingSequenceId = peakRequest.SequenceID;
                Logging.LogDebug("Sending waiting for sequence with sequenceId {Id}", peakRequest.SequenceID);
                WukongMpMod.Instance.SendWaitingForSequence(new SequenceWaitingData(peakRequest.SequenceID, client.LocalPlayerState.Location));
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