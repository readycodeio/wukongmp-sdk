using b1;
using ReadyM.Relay.Common;
using System.Linq;
using WukongMp.Api.DTO;
using WukongMp.Api.Patches;
using WukongMp.Api.UI;

namespace WukongMp.Api.WukongUtils;

public static class CutsceneUtils
{
    public static void PlayCutscene(PlayMovieData data)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BGW_EventCollection.Get(GameUtils.GetWorld());
            if (events == null)
            {
                Logging.LogError("Failed to get BGW_EventCollection");
                return;
            }

            events.Evt_RequestPlayMovie.Invoke(new FPlayMovieRequest
            {
                SequenceID = data.SequenceId,
                bDisablePlayerControl = data.DisablePlayerControl,
                bDisableMovementInput = data.DisableMovementInput,
                bDisableLookAtInput = data.DisableLookAtInput,
                bHidePlayer = data.HidePlayer,
                bHideHud = data.HideHud,
                OverlapBoxGuid = data.OverlapBoxGuid,
                MatchType = data.MatchType,
            });
        }, nameof(PlayCutscene));
    }

    public static void SetWaitingForCutsceneStatus(PlayerId playerId, SequenceWaitingData sequenceWaitingData)
    {
        Logging.LogDebug("Setting WaitingForCutsceneStatus for player: {Id}, sequenceId {SequenceId}", playerId, sequenceWaitingData.SequenceID);
        var player = DI.Instance.Players.GetPlayerById(playerId);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", playerId);
            return;
        }

        player.WaitingSequenceId = sequenceWaitingData.SequenceID;
        var localPlayer = DI.Instance.Players.LocalPlayerState;
        if (!localPlayer.IsWaitingForSequence)
        {
            localPlayer.SequenceLocation = sequenceWaitingData.SequenceLocation;
            localPlayer.IsJoiningSequence = true;
            InfoMessageWidget.Instance.SetVisibility(true);
            InfoMessageWidget.Instance.SetText("Join other players to proceed");
        }
    }

    public static void RequestSkipCurrentCutscene()
    {
        BGUFunctionLibraryCS.SkipCurrentSequence(GameUtils.GetWorld());
    }

    public static void SkipCutscene(int sequenceId)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            BGC_MovieData movieData = BGU_DataUtil.GetGameStateReadonlyData<BGC_MovieData>(GameUtils.GetWorld());
            MovieInstance cameraMovieInstance = movieData.CameraMovieInstance;
            if (cameraMovieInstance != null && cameraMovieInstance.CanSkipMovie() && cameraMovieInstance.SequenceId == sequenceId)
            {
                Logging.LogDebug("Skipping cutscene with sequenceId: {SequenceId}", sequenceId);
                cameraMovieInstance.SkipMovie();
            }
            else
            {
                Logging.LogWarning("Cannot skip cutscene, either not playing or sequenceId does not match. Current sequenceId: {CurrentSequenceId}, Requested: {RequestedSequenceId}",
                    cameraMovieInstance?.SequenceId, sequenceId);
            }
        }, nameof(SkipCutscene));
    }

    public static bool CheckAllPlayersWaitingForCutscene(int sequenceId)
    {
        return DI.Instance.Players.AllConnectedPlayers.All(p => p.WaitingSequenceId == sequenceId);
    }

    public static void TeleportLocalPlayerToCutsceneLocation()
    {
        var playerState = DI.Instance.Players.LocalPlayerState;
        if (playerState.IsJoiningSequence)
            PlayerUtils.TeleportLocalPlayer(playerState.SequenceLocation, playerState.Rotation, true);
    }
}