using ReadyM.Api.Idents;
using WukongMp.Api;
using WukongMp.Coop.Configuration;
using WukongMp.Coop.UI;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Patches;

public class CoopWidgetManager : IDisposable
{
    private readonly Lazy<CoopStatusWidget> _coopStatusWidget = new();

    internal void Initialize()
    {
        WukongApi.Events.OnJoinedArea += OnJoinedArea;
        WukongApi.Events.OnLeftArea += OnLeftArea;
        WukongApi.Events.OnOtherPlayerInsideArea += OnOtherPlayerInsideArea;
        WukongApi.Events.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideArea;

        WukongApi.Events.OnLevelLoaded += OnLevelLoaded;
        WukongApi.Events.OnExitLevel += OnExitLevel;
        WukongApi.Events.OnLoadingScreenClose += OnLoadingScreenClose;

        WukongApi.Events.OnPlayerChangedTeam += UpdatePlayerTeam;
        WukongApi.Events.OnLocalPlayerBeforeRebirth += OnLocalPlayerBeforeRebirth;
    }

    public void Dispose()
    {
        WukongApi.Events.OnJoinedArea -= OnJoinedArea;
        WukongApi.Events.OnLeftArea -= OnLeftArea;
        WukongApi.Events.OnOtherPlayerInsideArea -= OnOtherPlayerInsideArea;
        WukongApi.Events.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideArea;

        WukongApi.Events.OnLevelLoaded -= OnLevelLoaded;
        WukongApi.Events.OnExitLevel -= OnExitLevel;
        WukongApi.Events.OnLoadingScreenClose -= OnLoadingScreenClose;


        WukongApi.Events.OnPlayerChangedTeam -= UpdatePlayerTeam;
        WukongApi.Events.OnLocalPlayerBeforeRebirth -= OnLocalPlayerBeforeRebirth;
    }

    private void UpdatePlayerTeam(ReadyMainCharacter mainCharacter)
    {
        _coopStatusWidget.Value.RemovePlayer(mainCharacter.Nickname);
        _coopStatusWidget.Value.AddPlayer(mainCharacter.Nickname);
        RefreshWidgets();
    }

    private void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing co-op widgets");
        _coopStatusWidget.Value.Initialize();
    }

    private void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing co-op widgets");
        _coopStatusWidget.Value.Deinitialize();
    }

    private void OnLoadingScreenClose()
    {
        var isOnGameplayLevel = WukongApi.Client.CurrentAreaId != null;
        WukongApi.Widgets.ShowInGameWidgets(isOnGameplayLevel);

        if (isOnGameplayLevel)
        {
            _coopStatusWidget.Value.SetVisibility(true);
            _coopStatusWidget.Value.SetMaxConnectedCount(Constants.MaxPlayers);
        }
    }

    private void RefreshWidgets()
    {
        _coopStatusWidget.Value.SetConnectedCount(WukongApi.Client.AreaPlayers.Count);
        _coopStatusWidget.Value.SetMaxConnectedCount(Constants.MaxPlayers);
    }

    private void OnLocalPlayerBeforeRebirth()
    {
        WukongApi.Widgets.HideInfoMessage();
    }

    private void OnOtherPlayerInsideArea(PlayerId playerId, AreaId area)
    {
        if (WukongApi.Client.TryGetPlayerInfoById(playerId, out var nickname, out _))
        {
            _coopStatusWidget.Value.AddPlayer(nickname);
            RefreshWidgets();
        }
        else
        {
            Logging.LogWarning("Player entity for player {PlayerId} not found when they entered area {AreaId}, cannot add to co-op widget", playerId, area);
        }
    }

    private void OnOtherPlayerOutsideArea(PlayerId playerId, AreaId area)
    {
        if (WukongApi.Client.TryGetPlayerInfoById(playerId, out var nickname, out _))
        {
            _coopStatusWidget.Value.RemovePlayer(nickname);
            RefreshWidgets();
        }
    }

    private void OnJoinedArea(AreaId area)
    {
        if (WukongApi.Client.LocalPlayerId.HasValue &&
            WukongApi.Client.TryGetPlayerInfoById(WukongApi.Client.LocalPlayerId.Value, out var nickname, out _))
        {
            _coopStatusWidget.Value.AddPlayer(nickname);
            RefreshWidgets();
        }
        else
        {
            Logging.LogWarning("Local player entity not found when joining area {AreaId}, cannot add to co-op widget", area);
        }
    }

    private void OnLeftArea(AreaId area)
    {
        if (WukongApi.Client.LocalPlayerId.HasValue &&
            WukongApi.Client.TryGetPlayerInfoById(WukongApi.Client.LocalPlayerId.Value, out var nickname, out _))
        {
            _coopStatusWidget.Value.RemovePlayer(nickname);
            RefreshWidgets();
        }
    }
}