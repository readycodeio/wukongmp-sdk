using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.PvP.WukongUtils;

public static class PvpUtils
{
    public const string RedTeamColor = "(R=1,G=0.3,B=0.3)";
    public const string BlueTeamColor = "(R=0.3,G=0.3,B=1)";
    
    public static bool IsAfterLoadingScreen;

    public static void OnMatchmakingEnded()
    {
        if (IsAfterLoadingScreen)
        {
            SetupLobbyUi();
        }
    }

    public static void SetupLobbyUi()
    {
        if (!IsAfterLoadingScreen)
            return;

        if (Constants.IsPvP)
        {
            GameMessageWidget.Instance.SetVisibility(true);
            GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
            GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(DI.Instance.State.AllPlayers.Count, DI.Instance.PlayerState.LocalMainCharacter?.GetPvP().IsReadyForPvP == true));
            GameMessageWidget.Instance.SetThirdText(Texts.PressToSwitchTeam);
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

        if (Constants.IsPvP)
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
        if (areaEntity == null || !areaState.PvpState.HasValue)
            return;

        ref var room = ref areaEntity.Value.GetRoom();
        var current = areaState.PvpState.Value.CurrentRound;
        var total = room.TournamentRounds;
        UiUtils.ShowTip(string.Format(Texts.RoundCount, current, total), true);
    }

    public static string GetTeamColorString(int teamId)
    {
        if (teamId == Constants.AvailableTeamIds[0])
            return RedTeamColor;
        if (teamId == Constants.AvailableTeamIds[1])
            return BlueTeamColor;
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
    
    public static void CreatePvpStateEntity()
    {
        DI.Instance.AreaState.PvpStateEntity = DI.Instance.ClientNetEntity.CreateNetworkedAreaEntity(DI.Instance.ArchetypeRegistration.PvPStateSingletonArchetype).Entity;
    }
}