using System.Linq;
using b1;
using ReadyM.Relay.Client.State;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.WukongUtils;

public static class CutsceneUtils
{
    public static void PlayCutscene(PlayMovieRequestEvent ev)
    {
        var events = BGW_EventCollection.Get(GameUtils.GetWorld());
        if (events == null)
        {
            Logging.LogError("Failed to get BGW_EventCollection");
            return;
        }

        events.Evt_RequestPlayMovie.Invoke(new FPlayMovieRequest
        {
            SequenceID = ev.SequenceId,
            bDisablePlayerControl = ev.DisablePlayerControl,
            bDisableMovementInput = ev.DisableMovementInput,
            bDisableLookAtInput = ev.DisableLookAtInput,
            bHidePlayer = ev.HidePlayer,
            bHideHud = ev.HideHud,
            OverlapBoxGuid = ev.OverlapBoxGuid,
            MatchType = ev.MatchType,
        });
    }

    public static void SetJoiningCutsceneStatus(MainCharacterEntity mainEntity, WukongWidgetManager widgetManager, WaitingForSequenceEvent ev)
    {
        Logging.LogDebug("Setting JoiningCutsceneStatus for sequenceId {SequenceId}", ev.SequenceId);

        // FIXME: Cutscene data should be moved to PlayerEntity
        ref var localMain = ref mainEntity.GetLocalState();
        if (!localMain.IsWaitingForSequence)
        {
            localMain.JoiningSequenceLocation = ev.SequenceLocation;
            localMain.IsJoiningSequence = true;
            widgetManager.ShowInfoMessage(Texts.JoinOtherPlayersToProceed);
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

    public static bool CheckAllPlayersWaitingForCutscene(ClientState clientState, WukongPlayerState playerState, int sequenceId)
    {
        return clientState.AllPlayers.All(p => playerState.GetMainCharacterByPlayerId(p)?.GetState().WaitingSequenceId == sequenceId);
    }

    public static void TeleportLocalPlayerToCutsceneLocation(MainCharacterEntity mainEntity)
    {
        ref var main = ref mainEntity.GetState();
        ref var localMain = ref mainEntity.GetLocalState();
        if (localMain.IsJoiningSequence)
        {
            PlayerUtils.TeleportLocalPlayer(mainEntity, localMain.JoiningSequenceLocation, main.Rotation.ToFRotator(), false);
        }
    }

    public static void ClearLocalJoiningCutsceneStatus(MainCharacterEntity mainCharacter)
    {
        ref var localMain = ref mainCharacter.GetLocalState();
        BIC_MovieData? movieData = BGWGameInstanceCS.GetObject<BGW_GameDataMgr>(mainCharacter.Pawn)?.GetGameInstanceWritableData<BIC_MovieData>();
        movieData?.PlayMovieRequestQueue.Clear();

        localMain.IsJoiningSequence = false;
        localMain.IsWaitingForSequence = false;
    }
}