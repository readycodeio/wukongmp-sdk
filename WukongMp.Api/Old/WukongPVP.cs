using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using b1;
using BtlShare;
using CSharpModBase;
using ReadyM.Api.ECS.Idents;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.Enums;
using WukongMp.Api.Old.State;
using WukongMp.Api.Patches;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using PlayerState = WukongMp.Api.Old.State.PlayerState;

namespace WukongMp.Api.Old;

public partial class WukongPVP : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    
    private bool _isRoundEnding;
    private readonly Store _world;
    private readonly RoomStateProxy _roomState;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongPlayerPropertyManager _playerProperty;
    private readonly WukongEventBus _eventBus;
    private readonly WukongSynchronizer _synchronizer;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongChatter _chatter;

    public WukongPVP(
        Store world,
        RelaySerializer serializer,
        IRelayClient relayClient,
        RoomStateProxy roomState,
        WukongPlayerRegistry playerRegistry,
        WukongPlayerPropertyManager playerProperty,
        WukongEventBus eventBus,
        WukongSynchronizer synchronizer,
        WukongRpcCallbacks rpc,
        WukongChatter chatter
    )
    {
        _world = world;
        Serializer = serializer;
        RelayClient = relayClient;
        _roomState = roomState;
        _playerRegistry = playerRegistry;
        _playerProperty = playerProperty;
        _eventBus = eventBus;
        _synchronizer = synchronizer;
        _rpc = rpc;
        _chatter = chatter;

        _eventBus.OnBeginPlayGameplayLevel += OnBeginPlayGameplayLevel;
        _eventBus.OnLoadingScreenClose += OnLoadingScreenClose;

        _synchronizer.OnBeforeJoinedRoom += OnBeforeJoinedRoomHandler;
        _synchronizer.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
        _synchronizer.OnOtherPlayerJoined += OnOtherPlayerJoinedHandler;
        _synchronizer.OnOtherPlayerLeft += OnOtherPlayerLeftHandler;
        _synchronizer.OnPlayerPropertiesChanged += OnPlayerPropertiesChangedHandler;
        
        InitRpc();
    }

    public void Dispose()
    {
        DeInitRpc();
        
        _synchronizer.OnPlayerPropertiesChanged -= OnPlayerPropertiesChangedHandler;
        _synchronizer.OnOtherPlayerJoined -= OnOtherPlayerJoinedHandler;
        _synchronizer.OnOtherPlayerLeft -= OnOtherPlayerLeftHandler;
        _synchronizer.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
        _synchronizer.OnBeforeJoinedRoom -= OnBeforeJoinedRoomHandler;
        
        _eventBus.OnLoadingScreenClose -= OnLoadingScreenClose;
        _eventBus.OnBeginPlayGameplayLevel -= OnBeginPlayGameplayLevel;
    }

    public void StartPvP()
    {
        if (!RelayClient.IsMasterClient)
        {
            return;
        }

        // clear previous round winners
        _roomState.RoundWinners = [];

        Task.Run(StartRoundAsync);
    }
    
    public async Task StartRoundAsync()
    {
        if (!RelayClient.IsMasterClient)
        {
            Logging.LogError("Only master client can use the lobby manager");
            return;
        }

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        PlacePlayers(levelData.PvpStartingLocation, levelData.PvpRadius);
        await Task.Delay(100);

        SendPvPEvent(PvPEvent.RoundStart);
    }
    
    private void PlacePlayers(FVector center, float radius)
    {
        if (!RelayClient.IsMasterClient)
        {
            Logging.LogError("Only master client can use the lobby manager");
            return;
        }

        var playerStates = _playerRegistry.AllPvPPlayers.ToList();

        var teamsIds = playerStates.Select(playerState => playerState.TeamId).Distinct().ToList();
        var teamsCount = teamsIds.Count;
        var teamAngleStep = 2 * MathF.PI / teamsCount;

        var entityOffsetAngle = 0.15f;
        var teamMemberIndex = new Dictionary<int, int>();
        var teamIndex = new Dictionary<int, int>();
        for (var i = 0; i < teamsIds.Count; i++)
        {
            teamMemberIndex[teamsIds[i]] = 0;
            teamIndex[teamsIds[i]] = i;
        }

        foreach (var playerState in playerStates)
        {
            var teamBaseAngle = teamIndex[playerState.TeamId] * teamAngleStep;
            var memberIndex = teamMemberIndex[playerState.TeamId];

            var angle = teamBaseAngle + (memberIndex + 1) * entityOffsetAngle;
            var x = center.X + radius * MathF.Cos(angle);
            var y = center.Y + radius * MathF.Sin(angle);

            teamMemberIndex[playerState.TeamId]++;
            var newPlayerLocation = SpawningUtils.AdjustSpawnLocation(playerState.Pawn, new FVector(x, y, center.Z));
            var payload = new PlayerTransformData(playerState.PlayerId, newPlayerLocation, UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0, 0, 500)));
            _rpc.SendBroadcastPlayerTransform(payload);
        }
    }

    public async Task EndRoundAsync(int winner)
    {
        if (!RelayClient.IsMasterClient)
        {
            Logging.LogError("Only master client can use the lobby manager");
            return;
        }

        if (_isRoundEnding)
        {
            return;
        }

        _isRoundEnding = true;

        // disable pvp until next round
        SendPvPEvent(PvPEvent.RoundEnd, winner);

        // increment round number
        _roomState.SetLastRoundWinnerTeam(winner);

        // wait until all players death animations are finished
        await Task.Delay(5000);

        if (!RelayClient.IsMasterClient)
        {
            Logging.LogDebug("Master client disconnected before finishing EndRoundAsync");
            return;
        }

        await ResetHpAndRespawnAllPlayers();

        // resolve tournament
        var winnersSoFar = _roomState.RoundWinners.ToList();
        var winnersByTeam = winnersSoFar.Where(w => w != Constants.DrawTeamId).GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

        // check if only one team is present
        if (_playerRegistry.AllPvPPlayers.Select(p => p.TeamId).Distinct().Count() == 1)
        {
            SendPvPEvent(PvPEvent.TournamentEnd, winner);
            _isRoundEnding = false;
            return;
        }

        // check if any team won more than half of the rounds
        var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > _roomState.TournamentRounds / 2);
        if (winnerTeam.Key != 0)
        {
            SendPvPEvent(PvPEvent.TournamentEnd, winnerTeam.Key);
            _isRoundEnding = false;
            return;
        }

        // otherwise, check if we have a tie
        if (_roomState.CurrentRound > _roomState.TournamentRounds)
        {
            if (winnersByTeam.Count > 0)
            {
                // if any team have won more than others
                int maxWins = winnersByTeam.Values.Max();
                var winningTeams = winnersByTeam.Where(t => t.Value == maxWins).Select(t => t.Key).ToList();
                if (winningTeams.Count == 1)
                {
                    SendPvPEvent(PvPEvent.TournamentEnd, winningTeams[0]);
                }
                else
                {
                    SendPvPEvent(PvPEvent.TournamentEnd, Constants.DrawTeamId);
                }
            }
            else
            {
                // that was the final round
                SendPvPEvent(PvPEvent.TournamentEnd, Constants.DrawTeamId);
            }
        }
        else
        {
            // start next round
            await StartRoundAsync();
        }

        _isRoundEnding = false;
    }

    private async Task ResetHpAndRespawnAllPlayers()
    {
        if (!RelayClient.IsMasterClient)
        {
            Logging.LogError("Only master client can use the lobby manager");
            return;
        }

        // resurrect dead players and restore health to living ones
        SendPvPEvent(PvPEvent.ResetStats);
        foreach (var player in _playerRegistry.AllConnectedPlayers)
        {
            if (player.IsDead)
            {
                _rpc.SendRebirthPlayer(player.PlayerId);
            }
        }

        // wait for that to finish
        await Task.Delay(6500);
    }
    
    public void StartRound()
    {
        TimerWidget.Instance.StopCountdown();
        GameMessageWidget.Instance.SetVisibility(false);
        CountdownWidget.Instance.StopCountdown();
        TimerWidget.Instance.StartCountdown(Constants.RoundMinutes, Constants.RoundSeconds, RoundEndedTimeout);
        if (RelayClient.IsMasterClient)
        {
            _roomState.InCombatRound = true;

            var monsterCount = 0;
            _world.Query<LocalTamerComponent>().ForEachEntity((ref tamer, _) =>
            {
                if (tamer.IsTamerSynced)
                {
                    monsterCount++;
                }
            });

            if (_roomState.BotsEnabled && _playerRegistry.ConnectedPlayers.Count == 0 && monsterCount == 0)
            {
                GameLoopPatch.QueueOnGameThread(SpawningUtils.SpawnBots, "SpawnBots");
            }
        }
    }

    // NOTE: Renamed from OnRoundEnded to differentiate between event handlers for dependencies vs callbacks passed
    // to locally invoked methods.
    private void RoundEndedTimeout()
    {
        Logging.LogInformation("Round time ended, ending round");
        if (RelayClient.IsMasterClient)
        {
            Task.Run(async () => await EndRoundAsync(Constants.DrawTeamId));
        }
    }
    
    public void EndRound()
    {
        TimerWidget.Instance.StopCountdown();

        if (RelayClient.IsMasterClient)
        {
            _roomState.InCombatRound = false;
            foreach (var playerState in _playerRegistry.AllConnectedPlayers)
            {
                var events = BUS_EventCollectionCS.Get(playerState.Pawn);
                events?.Evt_RelieveImmobilized.Invoke();
                events?.Evt_RelievePhantomRush.Invoke();
            }
        }
    }
    
    public void ResetRoundState()
    {
        Utils.TryRunOnGameThread(TamerUtils.ClearEcsMonsters);
    }
    
    public void SetReadyState(bool isReady)
    {
        _playerProperty.CachePlayerProperty(nameof(PlayerState.IsReadyForPvP), isReady);
    }
    
    public void SwitchReadyState(bool isReady)
    {
        GameMessageWidget.Instance.SetThirdText(isReady ? Texts.YouAreReady : Texts.PressToSwitchTeam);
        GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(_playerRegistry.ConnectedPlayers.Count, isReady));
    }
    
    public void SwitchReadyStateMulti()
    {
        if (RelayClient.InRoom && _roomState is { InPvP: false, InMatchmaking: false } && _playerRegistry.ConnectedPlayers.Count > 0)
        {
            SwitchReadyState();
        }
    }

    public void SwitchReadyStateSingle()
    {
        if (RelayClient.InRoom && _roomState is { InPvP: false, InMatchmaking: false } && _playerRegistry.ConnectedPlayers.Count == 0)
        {
            SwitchReadyState();
        }
    }

    private void SwitchReadyState()
    {
        var isReady = _playerRegistry.LocalPlayerState.IsReadyForPvP;
        SetReadyState(!isReady);
        SwitchReadyState(!isReady);
    }
    
    public void SwitchTeam(bool force = false)
    {
        if (force || (RelayClient.InRoom && !_playerRegistry.LocalPlayerState.IsReadyForPvP && _roomState is { InPvP: false, InMatchmaking: false }))
        {
            var teamId = PvPUtils.GetOppositeTeam(_playerRegistry.LocalPlayerState.TeamId);
            _playerProperty.CachePlayerProperty(nameof(PlayerState.TeamId), teamId);
        }
    }
    
    public void EnablePvP()
    {
        Logging.LogInformation("Enabled PvP");

        var myTeam = _playerRegistry.LocalPlayerState.TeamId;
        var otherTeams = _playerRegistry.ConnectedPlayers.Values
            .Where(p => p.TeamId != myTeam)
            .Select(p => p.TeamId)
            .Distinct()
            .ToList();

        Logging.LogDebug("My team: {Team}", myTeam);
        Logging.LogDebug("Other teams: {Teams}", string.Join(", ", otherTeams));

        GameLoopPatch.QueueOnGameThread(() =>
        {
            foreach (var team in Constants.AvailableTeamIds)
            {
                ClientUtils.RegisterTeamHostility(myTeam, team);
            }
        }, "Register team hostility");
    }
    
    public void DisablePvP()
    {
        Logging.LogInformation("Disabled PvP");

        var myTeam = _playerRegistry.LocalPlayerState.TeamId;
        var otherTeams = _playerRegistry.ConnectedPlayers.Values
            .Where(p => p.TeamId != myTeam)
            .Select(p => p.TeamId)
            .Distinct()
            .ToList();

        Logging.LogDebug("My team: {Team}", myTeam);
        Logging.LogDebug("Other teams: {Teams}", string.Join(", ", otherTeams));

        GameLoopPatch.QueueOnGameThread(() =>
        {
            foreach (var team in Constants.AvailableTeamIds)
            {
                ClientUtils.UnregisterTeamHostility(myTeam, team);
            }
        }, "Register team hostility");
    }
    
    public void EnterPvP()
    {
        if (!RelayClient.IsMasterClient)
            return;

        if (!RelayClient.InRoom)
        {
            Logging.LogError("No room joined.");
            return;
        }

        _roomState.InPvP = true;
    }
    
    public void ExitPvP()
    {
        if (!RelayClient.IsMasterClient)
            return;

        if (!RelayClient.InRoom)
        {
            Logging.LogError("No room joined.");
            return;
        }

        _roomState.InPvP = false;
    }
    
    public void CheckRoundEndCondition()
    {
        if (!RelayClient.IsMasterClient || !_roomState.InPvP)
        {
            return;
        }

        // check if all players but one are dead
        var players = _playerRegistry.AllPvPPlayers.ToList();
        var aliveTeamIds = players.Where(p => !p.IsDead).Select(x => x.TeamId).ToList();

        var aliveMonsters = new List<int>();
        _world.Query<HpComponent, TeamComponent>().ForEachEntity((ref hp, ref team, _) =>
        {
            if (hp.Hp <= 0)
                return;

            aliveMonsters.Add(team.TeamId);
        });

        var alivePlayersTeams = aliveTeamIds.Concat(aliveMonsters).ToList();

        var aliveTeamCount = alivePlayersTeams.Distinct().Count();

        var aliveTeamPlayers = alivePlayersTeams
            .GroupBy(teamId => teamId)
            .Select(group => new { TeamId = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count).ToList();

        if (aliveTeamIds.Count == 0)
        {
            Logging.LogInformation("All players are dead, ending round");
            var aliveTeamId = aliveTeamPlayers.Count > 0 ? aliveTeamPlayers[0].TeamId : Constants.DrawTeamId;
            if (alivePlayersTeams.Count == 0)
            {
                Task.Run(async () => await EndRoundAsync(PvPUtils.GetOppositeTeam(aliveTeamId)));
            }
            else
            {
                Task.Run(async () => await EndRoundAsync(aliveTeamId));
            }

            return;
        }

        if (aliveTeamCount == 1)
        {
            Logging.LogInformation("One team with alive players, ending round");
            var winner = players.First(p => !p.IsDead);
            Task.Run(async () => await EndRoundAsync(winner.TeamId));
        }
    }
    
    public bool IsSkillEnabledInPVP(int skillId)
    {
        if (skillId == Constants.ImmobilizeSkillId && !_roomState.ImmobilizeAllowed)
        {
            return false;
        }

        // more skills here
        return true;
    }
    
    private void SetOrGetRoomPropsPVP()
    {
        Logging.LogInformation("Joining or creating private room");

        if (!RelayClient.IsMasterClient)
        {
            Logging.LogInformation("Not master client, skipping initialization");
            return;
        }

        // TODO: set from initial room properties (via server allocation request)
        _roomState.GameMode = GameMode.Private;
        _roomState.RoundWinners = [];
        _roomState.BotsEnabled = true; // TODO: Selector
        _roomState.MaxPlayers = 10;
    }
    
    private int GetSmallerTeamId()
    {
        Dictionary<int, int> teamsCount = [];
        var team1Id = Constants.AvailableTeamIds[0];
        var team2Id = Constants.AvailableTeamIds[1];
        teamsCount[team1Id] = 0;
        teamsCount[team2Id] = 0;
        
        foreach (var d in RelayClient.OtherPlayers)
        {
            if (d.Value.Properties.TryGetValue(nameof(PlayerState.TeamId), out var assignedTeamId))
            {
                Logging.LogDebug("Player {PlayerId} in team {TeamId}", d.Key, assignedTeamId);
                teamsCount[(int)assignedTeamId]++;
            }
        }

        return teamsCount[team1Id] > teamsCount[team2Id] ? team2Id : team1Id;
    }
    
    private void SetUpRoom()
    {
        if (RelayClient.IsMasterClient)
        {
            _roomState.InPvP = false;
        }
    }
    
    private void SetupMatchmaking()
    {
        if (_roomState.GameMode == GameMode.Private)
            return;

        if (RelayClient.IsMasterClient)
        {
            _roomState.InMatchmaking = true;
            _roomState.MatchmakingEndTime = DateTime.UtcNow.AddSeconds(Constants.MatchmakingSeconds).Ticks;
        }
    }

    private FVector GetSpawnPosition(PlayerId playerId)
    {
        int maxPlayersCount = _roomState.MaxPlayers;

        float angle = playerId.RawValue / (float)maxPlayersCount * 2f * FMath.PI;
        float x = FMath.Cos(angle) * Constants.PvpStartingRadius;
        float y = FMath.Sin(angle) * Constants.PvpStartingRadius;

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        var baseLocation = levelData.PvpStartingLocation + new FVector(x, y, 0f);
        return SpawningUtils.AdjustSpawnLocation(_playerRegistry.GetPlayerById(playerId)?.Pawn, baseLocation);
    }
    
    private void SetupAddedPlayer(PlayerId playerId)
    {
        var playerState = _playerRegistry.GetPlayerById(playerId);

        if (playerState != null)
        {
            var props = RelayClient.GetPlayerState(playerId)?.Properties;

            if (props == null)
            {
                Logging.LogError("Player properties are null");
                return;
            }

            // set IsSpectator if client should be (joining during fight)
            var isSpectator = playerState.IsSpectator;

            if (!isSpectator)
            {
                playerState.IsSpectator = _roomState.InPvP && !playerState.IsReadyForPvP;
            }

            // readiness callback
            if (playerState.IsReadyForPvP)
            {
                NotifyPlayerReadinessChanged(playerState.NickName, playerState.IsReadyForPvP);
            }

            if (_playerRegistry.AllConnectedPlayers.Count() == _roomState.MaxPlayers)
            {
                EndMatchmaking();
            }
        }
    }
    
    private void EndMatchmaking()
    {
        if (RelayClient.IsMasterClient)
        {
            _roomState.InMatchmaking = false;
            _rpc.SendEndMatchmaking();
        }

        TimerWidget.Instance.StopCountdown();
    }
    
    public void NotifyPlayerReadinessChanged(string playerNickname, bool isReady)
    {
        var playersReadyCount = _playerRegistry.ConnectedPlayers.Values.Count(x => x.IsReadyForPvP) + (_playerRegistry.LocalPlayerState.IsReadyForPvP ? 1 : 0);
        GameLoopPatch.QueueOnGameThread(() => UpdateReadiness(playerNickname, isReady, playersReadyCount));
    }
    
    private void UpdateReadiness(string playerNickName, bool isReady, int readyCount)
    {
        if (RelayClient.IsMasterClient) // send this only once
        {
            if (isReady)
            {
                _chatter.SendServerMessage("PlayerIsReady", playerNickName);
            }
            else
            {
                _chatter.SendServerMessage("PlayerIsNotReady", playerNickName);
            }
        }

        if (isReady)
        {
            if ((_playerRegistry.ConnectedPlayers.Count > 0 || _roomState.BotsEnabled) && readyCount == _playerRegistry.ConnectedPlayers.Count + 1)
            {
                // all players are ready
                GameMessageWidget.Instance.SetMainText(Texts.StartingGame);
                CountdownWidget.Instance.StartLobbyCountdown(Constants.CountdownSeconds, StartPvP);
            }

            LobbyStatusWidget.Instance.SetReadyCount(readyCount);
        }
        else
        {
            CountdownWidget.Instance.StopCountdown();
            GameMessageWidget.Instance.SetMainText(Texts.InMultiplayer);
            LobbyStatusWidget.Instance.SetReadyCount(readyCount);
        }
    }
    
    #region Event Handlers
    
    private void OnBeginPlayGameplayLevel()
    {
        TamerUtils.DestroyAllTamers();
    }
    
    private void OnLoadingScreenClose()
    {
        if (RelayClient.InRoom)
        {
            PvPUtils.IsAfterLoadingScreen = true;
            if (_roomState.InMatchmaking)
            {
                var timeDifference = new DateTime(_roomState.MatchmakingEndTime, DateTimeKind.Utc) - DateTime.UtcNow;
                TimerWidget.Instance.StartCountdown(0, timeDifference.Seconds, EndMatchmaking);
                PvPUtils.SetupMatchmakingUi();
            }
            else if (!_playerRegistry.LocalPlayerState.IsSpectator)
            {
                PvPUtils.SetupLobbyUi();
            }
        }
    }

    private void OnBeforeJoinedRoomHandler()
    {
        SetOrGetRoomPropsPVP();

        Logging.LogInformation("Joined room");

        var teamId = (int)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.TeamId), GetSmallerTeamId());
        _playerProperty.CachePlayerProperty(nameof(PlayerState.TeamId), teamId);
        
        _playerRegistry.LocalPlayerState.IsReadyForPvP = (bool)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.IsReadyForPvP), false);

        SetUpRoom();
        LobbyStatusWidget.Instance.SetReadyCount(_playerRegistry.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
        SetupMatchmaking();
    }
    
    private void OnAfterJoinedRoomHandler()
    {
        var spawnPosition = GetSpawnPosition(_playerRegistry.LocalPlayerState.PlayerId);
        var data = new PlayerTransformData(_playerRegistry.LocalPlayerState.PlayerId, spawnPosition, FRotator.ZeroRotator);
        _rpc.OnBroadcastPlayerTransform(data);
    }
    
    private void OnOtherPlayerJoinedHandler(PlayerId playerId)
    {
        Logging.LogInformation("Player {PlayerId} entered the room", playerId);
        GameLoopPatch.QueueOnGameThread(() => SetupAddedPlayer(playerId), "AddPlayer");
    }
    
    private void OnOtherPlayerLeftHandler(PlayerId playerId)
    {
        if (RelayClient.IsMasterClient)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(Constants.PlayerTtlMs);
                CheckRoundEndCondition();
            });
        }
    }

    private void OnPlayerPropertiesChangedHandler(PlayerId playerId, Dictionary<object, object?> changes)
    {
        if (playerId == RelayClient.LocalPlayer.PlayerId) // local player
        {
            if (!_playerRegistry.HasLocalPlayerState)
            {
                Logging.LogWarning("Local player state is null.");
                return;
            }
        }
        else if (!_playerRegistry.ConnectedPlayers.ContainsKey(playerId))
        {
            Logging.LogDebug("Player {Id} not found.", playerId); // TODO: Investigate why this is spammed
            return;
        }

        foreach (var kvp in changes)
        {
            if (kvp.Value == null)
                continue; // we don't really handle property removal

            if (kvp.Key is not string propertyName)
            {
                // ignore system properties
                continue;
            }

            // special handlers for some properties
            switch (propertyName)
            {
                case nameof(PlayerState.IsReadyForPvP):
                    var state = RelayClient.GetPlayerState(playerId);

                    if (state == null)
                    {
                        Logging.LogError("Player {Id} not found.", playerId);
                        continue;
                    }

                    var targetPlayerNickname = (string)state.Properties[nameof(PlayerState.NickName)];
                    NotifyPlayerReadinessChanged(targetPlayerNickname, (bool)kvp.Value);
                    break;
            }
        }
    }
    
    #endregion
    
    #region RPC
    
    public void SendPvPEvent(PvPEvent ev, int data = 0)
    {
        if (!RelayClient.IsMasterClient)
        {
            Logging.LogError("Only room owner can send start countdown.");
            return;
        }

        Logging.LogInformation("Sending PvP event: {Event}", ev);

        SendPvpEvent([(int)ev, data]);
    }
    
    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnPvpEvent(int[] data)
    {
        // TODO: Not QueueOnGameThread, why?
        var ev = (PvPEvent)data[0];
        var winnerTeamId = data[1];

        Logging.LogDebug("Received PvP event: {Event}", ev);

        switch (ev)
        {
            case PvPEvent.RoundStart:
                Task.Run(PvPUtils.ShowPvPCountDown);
                StartRound();
                EnablePvP();
                EnterPvP();
                break;
            case PvPEvent.RoundEnd:
                DisablePvP();
                EndRound();

                if (winnerTeamId == Constants.DrawTeamId)
                {
                    UIUtils.ShowTip(Texts.RoundDraw);
                }
                else
                {
                    UIUtils.ShowTip(string.Format(Texts.RoundEndedWinner, PvPUtils.GetLocalizedTeamName(winnerTeamId)));
                }

                if (winnerTeamId == Constants.DrawTeamId)
                    return;

                if (winnerTeamId == _playerRegistry.LocalPlayerState.TeamId)
                {
                    AssetUtils.PlayBossDefeatedSound();
                }

                break;
            case PvPEvent.TournamentEnd:
            {
                if (winnerTeamId == Constants.DrawTeamId)
                {
                    UIUtils.ShowTip(Texts.TournamentDraw);
                }
                else
                {
                    UIUtils.ShowTip(string.Format(Texts.TournamentEndedWinner, PvPUtils.GetLocalizedTeamName(winnerTeamId)));
                }

                Task.Run(async () =>
                {
                    if (RelayClient.IsMasterClient)
                    {
                        foreach (var playerState in _playerRegistry.SpectatingPlayers)
                        {
                            _playerProperty.SetRemotePlayerProperty(playerState.PlayerId, nameof(PlayerState.IsSpectator), false);
                        }
                    }

                    await Task.Delay(2000);
                    PvPUtils.EndTournament();
                    ExitPvP();
                    _playerRegistry.LocalPlayerState.IsReadyForPvP = false;
                    SetReadyState(false);
                });

                break;
            }
            case PvPEvent.ResetStats:
                ResetRoundState();

                if (!_playerRegistry.LocalPlayerState.IsDead)
                {
                    Utils.TryRunOnGameThread(() =>
                    {
                        TamerUtils.DestroyAllTamers();
                        var events = BUS_EventCollectionCS.Get(_playerRegistry.LocalPlayerState.Pawn!);

                        if (events == null)
                        {
                            Logging.LogError("events are null");
                            return;
                        }

                        events.Evt_TriggerTeleportResetPlayer!.Invoke();
                    });
                }

                if (RelayClient.IsMasterClient)
                {
                    // reset other players' Hp to HpMax if they are not dead
                    foreach (var (key, state) in _playerRegistry.ConnectedPlayers)
                    {
                        if (!state.IsDead)
                        {
                            if (state.Pawn == null)
                            {
                                Logging.LogError("Pawn is null in {Patch}", nameof(OnPvpEvent));
                                return;
                            }

                            var attrContainer = (BUC_AttrContainer?)BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(state.Pawn);
                            if (attrContainer != null)
                            {
                                var hpMax = attrContainer.GetFloatValue(EBGUAttrFloat.HpMax);
                                attrContainer.SetFloatValue(EBGUAttrFloat.Hp, hpMax);
                                state.Hp = hpMax;
                                _playerProperty.SetRemotePlayerProperty(key, nameof(PlayerState.Hp), state.Hp);
                            }
                        }
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ev));
        }
    }

    #endregion
}