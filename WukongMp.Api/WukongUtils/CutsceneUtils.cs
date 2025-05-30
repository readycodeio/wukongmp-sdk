using b1;
using System.Linq;
using WukongMp.Api.DTO;
using WukongMp.Api.Old;
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

    public static void SetWaitingForCutsceneStatus(short playerId, int sequenceId)
    {
        var player = WukongMpModBase.Client.GetPlayerById(playerId);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", playerId);
            return;
        }

        player.WaitingSequenceId = sequenceId;
        if (!WukongMpModBase.Client.LocalPlayerState.IsWaitingForSequence)
        {
            InfoMessageWidget.Instance.SetVisibility(true);
            InfoMessageWidget.Instance.SetText("Join other players to proceed");
        }
    }

    public static void SkipCurrentCutscene()
    {
        BGUFunctionLibraryCS.SkipCurrentSequence(GameUtils.GetWorld());
    }

    public static bool CheckAllPlayersWaitingForCutscene(int sequenceId)
    {
        return WukongMpModBase.Client.AllConnectedPlayers.All(p => p.WaitingSequenceId == sequenceId);
    }

    public static void TeleportLocalPlayerToCutsceneLocation()
    {
        var playerState = WukongMpModBase.Client.LocalPlayerState;
        if (playerState.IsJoiningSequence)
            PlayerUtils.TeleportLocalPlayer(playerState.SequenceLocation, playerState.Rotation, true);
    }
}