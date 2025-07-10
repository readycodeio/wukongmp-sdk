using WukongMp.Api.Configuration;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old;

public class WukongPlayerModeManager(WukongPlayerRegistry playerRegistry, RoomStateProxy roomState)
{
    public void HandleBecameSpectator(PlayerState playerState)
    {
        var isMyself = playerState.PlayerId == playerRegistry.LocalPlayerState.PlayerId;

        if (isMyself)
            UIUtils.SetHudVisibility(false);

        SetPlayerVisibility(playerState, false);

        if (isMyself)
        {
            FreeCameraManager.Instance.EnterFreeCameraMode();
            PvPUtils.SetupSpectatorUi();
        }

        UpdatePlayerTeamUi(playerState);
    }

    public void HandleStoppedBeingSpectator(PlayerState playerState)
    {
        var isMyself = playerState.PlayerId == playerRegistry.LocalPlayerState.PlayerId;

        if (isMyself)
            UIUtils.SetHudVisibility(true);

        SetPlayerVisibility(playerState, true);

        if (isMyself)
        {
            FreeCameraManager.Instance.LeaveFreeCameraMode();
            if (roomState.InMatchmaking)
            {
                PvPUtils.SetupMatchmakingUi();
            }
            else if (!roomState.InPvP)
            {
                PvPUtils.SetupLobbyUi();
            }
            else
            {
                LobbyStatusWidget.Instance.SetVisibility(false);
                CoopStatusWidget.Instance.SetVisibility(false);
            }
        }

        UpdatePlayerTeamUi(playerState);
    }

    public void SetPlayerVisibility(PlayerState playerState, bool visible)
    {
        Logging.LogDebug("Setting player {PlayerName} visibility to: {Visibility}", playerState.NickName, visible);

        if (playerState.Pawn == null)
        {
            Logging.LogError("Player pawn is null");
            return;
        }

        playerState.Pawn.SetActorHiddenInGame(!visible);
        playerState.MarkerActor?.SetActorHiddenInGame(!visible);
    }
    
    public void UpdatePlayerTeam(PlayerState playerState, int teamId)
    {
        Logging.LogDebug("Updating player {Nickname} to team {Team}", playerState.NickName, teamId);

        var player = playerState.Pawn;

        if (player == null)
        {
            Logging.LogError("Failed to cast pawn to BGUCharacterCS");
            return;
        }

        ClientUtils.RegisterNewPlayerTeam(player, teamId);

        if (playerState.MarkerActor != null)
        {
            var teamColor = Constants.IsCoop ? Constants.WhiteTeamColor : PvPUtils.GetTeamColorString(playerState.TeamId);
            playerState.MarkerActor.CallFunctionByNameWithArguments($"SetText {playerState.NickName} {teamColor}", true);
        }

        UpdatePlayerTeamUi(playerState);
    }

    public void UpdatePlayerTeamUi(PlayerState playerState)
    {
        if (Constants.IsCoop)
        {
            CoopStatusWidget.Instance.RemovePlayer(playerState.NickName);
            CoopStatusWidget.Instance.AddPlayer(playerState.NickName);
        }
        else
        {
            LobbyStatusWidget.Instance.UpdatePlayerTeam(playerState.NickName, playerState.TeamId, playerState.IsSpectator);
        }
    }
}