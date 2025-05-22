using System.Reflection;
using b1;
using HarmonyLib;

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
