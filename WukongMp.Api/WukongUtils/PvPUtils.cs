using CSharpModBase;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;

namespace WukongMp.Api.WukongUtils;

public static class PvPUtils
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
            GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(DI.Instance.State.AllPlayers.Count, DI.Instance.PlayerState.LocalPlayer?.GetState().IsReadyForPvP == true));
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

    public static void ShowPvPCountDown()
    {
        var areaState = DI.Instance.AreaState;
        var areaEntity = areaState.CurrentArea;
        if (areaEntity == null)
            return;

        ref var room = ref areaEntity.Value.GetRoom();
        var current = room.CurrentRound;
        var total = room.TournamentRounds;
        UIUtils.ShowTip(string.Format(Texts.RoundCount, current, total));
    }

    public static string GetTeamColorString(int teamId)
    {
        if (teamId == Constants.AvailableTeamIds[0])
            return Constants.RedTeamColor;
        if (teamId == Constants.AvailableTeamIds[1])
            return Constants.BlueTeamColor;
        return "";
    }

    public static string GetLocalizedTeamName(int teamId)
    {
        if (teamId == Constants.AvailableTeamIds[0])
            return Texts.RedTeam;
        if (teamId == Constants.AvailableTeamIds[1])
            return Texts.BlueTeam;
        return "";
    }

    public static int GetOppositeTeam(int teamId)
    {
        if (teamId == Constants.DrawTeamId)
            return teamId;
        return teamId == Constants.AvailableTeamIds[0] ? Constants.AvailableTeamIds[1] : Constants.AvailableTeamIds[0];
    }

    public static void EndTournament()
    {
        Logging.LogInformation("End tournament");
        SetupLobbyUi();
    }
}