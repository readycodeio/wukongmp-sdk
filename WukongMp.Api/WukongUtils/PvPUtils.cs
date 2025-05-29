using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;

namespace WukongMp.Api.WukongUtils;

public class PvPUtils
{
    public static bool IsAfterLoadingScreen;

    public static void OnMatchmakingEnded()
    {
        TimerWidget.Instance.StopCountdown();
        if (IsAfterLoadingScreen)
        {
            SetupLobbyUi();
        }
    }

    public static void SetupLobbyUi()
    {
        if (!IsAfterLoadingScreen)
            return;

        if (!Constants.IsCoop)
        {
            GameMessageWidget.Instance.SetVisibility(true);
            GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
            GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(WukongMpModBase.Client.ConnectedPlayers.Count, WukongMpModBase.Client.LocalPlayerState.IsReadyForPvP));
            GameMessageWidget.Instance.SetThirdText(Texts.PressToSwitchTeam);
            LobbyStatusWidget.Instance.SetVisibility(true);
        }
        else
        {
            CoopStatusWidget.Instance.SetVisibility(true);
        }
    }

    public static void SetupMatchmakingUi()
    {
        if (!IsAfterLoadingScreen)
            return;

        GameMessageWidget.Instance.SetVisibility(true);
        GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
        GameMessageWidget.Instance.SetSecondText(Texts.MatchmakingInProgress);
        GameMessageWidget.Instance.SetThirdText("");
        if (!Constants.IsCoop)
        {
            LobbyStatusWidget.Instance.SetVisibility(true);
        }
        else
        {
            CoopStatusWidget.Instance.SetVisibility(true);
        }
    }

    public static void SetupSpectatorUi()
    {
        if (!IsAfterLoadingScreen)
            return;

        if (!Constants.IsCoop)
        {
            GameMessageWidget.Instance.SetVisibility(true);
            GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
            GameMessageWidget.Instance.SetSecondText(Texts.WaitForEnd);
            GameMessageWidget.Instance.SetThirdText("");
            LobbyStatusWidget.Instance.SetVisibility(true);
        }
    }

    public static void EndTournament()
    {
        Logging.LogInformation("End tournament");
        SetupLobbyUi();
    }
}