using b1;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnrealEngine.Runtime;

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

[HarmonyPatch(typeof(BGW_MovieManager), "RequestPlayMovie")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRequestPlayMovie
{
    public static void Postfix(ref FPlayMovieRequest InRequest)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        if (WukongMP.Instance.Client.IsMasterClient)
        {
            Logging.LogDebug("BroadRequesting movie with sequenceId {Id}", InRequest.SequenceID);
            WukongMP.Instance.Client.SendPlayMovieRequest(InRequest);
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
        if (GlobalMovieData.PlayMovieRequestQueue.Count > 0 && WukongMP.Instance.AreAllPlayersNearby())
        {
            GameUtils.HideTip();
            while (GlobalMovieData.PlayMovieRequestQueue.Count > 0)
            {
                RequestPlayMovieMethod?.Invoke(__instance, [GlobalMovieData.PlayMovieRequestQueue.Dequeue()]);
            }
        }
        else
        {
            GameUtils.ShowTip("Wait for other players");
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