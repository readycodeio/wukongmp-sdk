using b1;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnrealEngine.Runtime;
using WukongMp.Api.UI;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnPlayMovieInstance
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_MovieSystem:OnPlayMovieInstance");
    }

    public static void Postfix(int SequenceId, MovieInstance Instance)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        Instance.MarkCanBeSkipped(true);
        Logging.LogDebug("Playing movie {Name} with sequenceId {Id}", Instance.GetName(), SequenceId);
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

        if (!UBGWFunctionLibraryCS.HasSequenceAlreadyPlayed(__instance.GetOwner(), Request.SequenceID) && Request.bDisablePlayerControl == true)
        {
            Logging.LogDebug("BroadRequesting movie with sequenceId {Id}", Request.SequenceID);
            WukongMP.Instance.Client.SendPlayMovieRequest(Request);
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;

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
            if (client.LocalPlayerState.JoiningSequenceId == peakRequest.SequenceID || WukongMP.Instance.ArePlayersCloseToSyncCutscene() || peakRequest.bDisablePlayerControl == false)
            {
                InfoMessageWidget.Instance.SetVisibility(false);
                client.LocalPlayerState.HasRestrictedMovement = false;
                client.LocalPlayerState.IsWaitingForMovie = false;
                client.LocalPlayerState.JoiningSequenceId = 0;

                while (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
                {
                    RequestPlayMovieMethod?.Invoke(__instance, [GlobalMovieData.PlayMovieRequestQueue.Dequeue()]);
                    BGW_EventCollection.Get(__instance.GetOwner()).Evt_MarkMoviePlayed(peakRequest.SequenceID);
                }
            }
            else if (!client.LocalPlayerState.IsWaitingForMovie)
            {
                InfoMessageWidget.Instance.SetVisibility(true);
                InfoMessageWidget.Instance.SetText("Wait for other players");
                client.LocalPlayerState.HasRestrictedMovement = true;
                client.LocalPlayerState.IsWaitingForMovie = true;
                client.LocalPlayerState.RestrictionPoint = client.LocalPlayerState.Location;
                client.SendWaitingForMovie(peakRequest.SequenceID);
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