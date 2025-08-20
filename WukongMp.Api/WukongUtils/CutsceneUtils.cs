using System.Linq;
using b1;
using ReadyM.Api.Multiplayer.Idents;
using WukongMp.Api.DTO;
using WukongMp.Api.UI;

namespace WukongMp.Api.WukongUtils;

public static class CutsceneUtils
{
    public static void PlayCutscene(PlayMovieData data)
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
    }

    public static void SetWaitingForCutsceneStatus(PlayerId playerId, SequenceWaitingData sequenceWaitingData)
    {
        Logging.LogDebug("Setting WaitingForCutsceneStatus for player: {Id}, sequenceId {SequenceId}", playerId, sequenceWaitingData.SequenceID);
        var mainEntity = DI.Instance.PlayerState.GetMainCharacterById(playerId);
        if (mainEntity == null)
        {
            Logging.LogError("Player not found: {Id}", playerId);
            return;
        }

        ref var localMain = ref mainEntity.Value.GetLocalState();
        
        localMain.WaitingSequenceId = sequenceWaitingData.SequenceID;
        if (!localMain.IsWaitingForSequence)
        {
            localMain.SequenceLocation = sequenceWaitingData.SequenceLocation;
            localMain.IsJoiningSequence = true;
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
    }

    public static bool CheckAllPlayersWaitingForCutscene(int sequenceId)
    {
        var playerState = DI.Instance.PlayerState;
        return DI.Instance.State.AllPlayers.All(p => playerState.GetMainCharacterById(p)?.GetLocalState().WaitingSequenceId == sequenceId);
    }

    public static void TeleportLocalPlayerToCutsceneLocation()
    {
        var playerState = DI.Instance.PlayerState;
        var mainEntity = playerState.LocalMainCharacter;
        if (mainEntity == null)
            return;
        ref var main = ref mainEntity.Value.GetState();
        ref var localMain = ref mainEntity.Value.GetLocalState();
        if (localMain.IsJoiningSequence)
            PlayerUtils.TeleportLocalPlayer(mainEntity.Value, localMain.SequenceLocation, main.Rotation.ToFRotator(), true);
    }
}