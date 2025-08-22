using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using b1;
using BtlShare;
using CSharpModBase;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Values;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.PVP;

public partial class WukongPVP : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    
    private bool _isRoundEnding;
    private readonly Store _world;
    private readonly ClientState _state;
    private readonly WukongAreaState _areaState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongEventBus _eventBus;
    private readonly WukongSynchronizer _synchronizer;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongChatter _chatter;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly ILogger _logger;

    private (PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)? GetEntities(PlayerId playerId)
    {
        var playerEntity = _playerState.GetPlayerById(playerId);
        var mainEntity = _playerState.GetMainCharacterById(playerId);
        if (!playerEntity.HasValue || !mainEntity.HasValue)
            return null;
        return (PlayerId: playerId, Player: playerEntity.Value, Character: mainEntity.Value);
    }
    
    // FIXME: The originals used ConnectedPlayers which seem to have not contained the LocalPlayer. Recheck the logic.
    
    public IEnumerable<PlayerId> SpectatingPlayerIds
        => _state.AllPlayers.Where(p => _playerState.GetPlayerById(p)?.GetState().IsSpectator == true);

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> SpectatingPlayers
        => SpectatingPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<PlayerId> AllPvPPlayerIds
        => _state.AllPlayers.Where(p => _playerState.GetPlayerById(p)?.GetState().IsSpectator == false);

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> AllPvPPlayers
        => AllPvPPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> AllPlayers
        => _state.AllPlayers.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<PlayerId> OtherPlayerIds
        => _state.AllPlayers.Where(p => p != _state.LocalPlayerId);
    
    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> OtherPlayers
        => OtherPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();
    
    public WukongPVP(
        Store world,
        RelaySerializer serializer,
        IRelayClient relayClient,
        ClientState state,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongEventBus eventBus,
        WukongSynchronizer synchronizer,
        WukongRpcCallbacks rpc,
        WukongChatter chatter,
        IClientEcsUpdateLoop ecsLoop,
        ILogger logger
    )
    {
        _world = world;
        Serializer = serializer;
        RelayClient = relayClient;
        _state = state;
        _areaState = areaState;
        _playerState = playerState;
        _eventBus = eventBus;
        _synchronizer = synchronizer;
        _rpc = rpc;
        _chatter = chatter;
        _ecsLoop = ecsLoop;
        _logger = logger;

        _eventBus.OnBeginPlayGameplayLevel += OnBeginPlayGameplayLevel;
        _eventBus.OnLoadingScreenClose += OnLoadingScreenClose;

        _state.OnJoinedArea += OnJoinedAreaHandler;
        _state.OnOtherPlayerInsideArea += OnOtherPlayerInsideAreaHandler;
        _state.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideAreaHandler;

        _ecsLoop.OnUpdateLoop += OnUpdateLoopHandler;
        
        InitRpc();
    }

    public void Dispose()
    {
        DeInitRpc();
        
        _ecsLoop.OnUpdateLoop -= OnUpdateLoopHandler;
        
        _state.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideAreaHandler;
        _state.OnOtherPlayerInsideArea -= OnOtherPlayerInsideAreaHandler;
        _state.OnJoinedArea -= OnJoinedAreaHandler;
        
        _eventBus.OnLoadingScreenClose -= OnLoadingScreenClose;
        _eventBus.OnBeginPlayGameplayLevel -= OnBeginPlayGameplayLevel;
    }

    public void StartPvP()
    {
        if (!_areaState.IsMasterClient)
            return;

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        ref var room = ref areaEntity.Value.GetRoom();
        
        // clear previous round winners
        room.RoundWinners = [];

        Task.Run(StartRoundAsync);
    }
    
    public async Task StartRoundAsync()
    {
        if (!_areaState.IsMasterClient)
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
        if (!_areaState.IsMasterClient)
        {
            Logging.LogError("Only master client can use the lobby manager");
            return;
        }

        var playerEntities = AllPvPPlayers.ToList();

        var teamsIds = playerEntities.Select(p => p.Player.GetState().TeamId).Distinct().ToList();
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

        foreach (var (playerId, _, mainEntity) in playerEntities)
        {
            ref var localMainComp = ref mainEntity.GetLocalState();
            ref readonly var teamComp = ref mainEntity.GetTeam();
            
            var teamBaseAngle = teamIndex[teamComp.TeamId] * teamAngleStep;
            var memberIndex = teamMemberIndex[teamComp.TeamId];

            var angle = teamBaseAngle + (memberIndex + 1) * entityOffsetAngle;
            var x = center.X + radius * MathF.Cos(angle);
            var y = center.Y + radius * MathF.Sin(angle);

            teamMemberIndex[teamComp.TeamId]++;
            var newPlayerLocation = SpawningUtils.AdjustSpawnLocation(localMainComp.Pawn, new FVector(x, y, center.Z));
            var payload = new PlayerTransformData(playerId, newPlayerLocation, UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0, 0, 500)));
            _rpc.SendBroadcastPlayerTransform(payload);
        }
    }

    public async Task EndRoundAsync(int winner)
    {
        if (!_areaState.IsMasterClient)
        {
            Logging.LogError("Only master client can use the lobby manager");
            return;
        }

        if (_isRoundEnding)
            return;

        _isRoundEnding = true;

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("Current area is null, cannot end round");
            return;
        }
        
        // disable pvp until next round
        SendPvPEvent(PvPEvent.RoundEnd, winner);

        // increment round number
        areaEntity.Value.GetRoom().SetLastRoundWinnerTeam(winner);

        // wait until all players death animations are finished
        await Task.Delay(5000);

        if (!_areaState.IsMasterClient)
        {
            Logging.LogDebug("Master client disconnected before finishing EndRoundAsync");
            return;
        }

        await ResetHpAndRespawnAllPlayers();

        // resolve tournament
        var winnersSoFar = areaEntity.Value.GetRoom().RoundWinners.ToList();
        var winnersByTeam = winnersSoFar.Where(w => w != Constants.DrawTeamId).GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

        // check if only one team is present
        if (AllPvPPlayers.Select(p => p.Player.GetState().TeamId).Distinct().Count() == 1)
        {
            SendPvPEvent(PvPEvent.TournamentEnd, winner);
            _isRoundEnding = false;
            return;
        }

        // check if any team won more than half of the rounds
        var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > areaEntity.Value.GetRoom().TournamentRounds / 2);
        if (winnerTeam.Key != 0)
        {
            SendPvPEvent(PvPEvent.TournamentEnd, winnerTeam.Key);
            _isRoundEnding = false;
            return;
        }

        // otherwise, check if we have a tie
        if (areaEntity.Value.GetRoom().CurrentRound > areaEntity.Value.GetRoom().TournamentRounds)
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
        if (!_areaState.IsMasterClient)
        {
            Logging.LogError("Only master client can use the lobby manager");
            return;
        }

        // resurrect dead players and restore health to living ones
        SendPvPEvent(PvPEvent.ResetStats);
        foreach (var (playerId, _, mainEntity) in AllPlayers)
        {
            if (mainEntity.GetState().IsDead)
            {
                _rpc.SendRebirthPlayer(playerId);
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
        if (!_areaState.IsMasterClient)
            return;
        
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        ref var room = ref areaEntity.Value.GetRoom();
        
        room.InCombatRound = true;

        var monsterCount = 0;
        _world.Query<LocalTamerComponent>().ForEachEntity((ref LocalTamerComponent localTamerComp, Entity _) =>
        {
            if (localTamerComp.IsTamerSynced)
            {
                monsterCount++;
            }
        });

        if (room.BotsEnabled && !OtherPlayers.Any() && monsterCount == 0)
        {
            // FIXME: Is there any way to get rid of having those checks all over the place? Seems very tedious,
            // and it handles a fringe case where somehow the player got disconnected.
            if (_playerState.LocalPlayerEntity != null)
                SpawningUtils.SpawnBots(_playerState.LocalPlayerEntity.Value);
        }
    }

    // NOTE: Renamed from OnRoundEnded to differentiate between event handlers for dependencies vs callbacks passed
    // to locally invoked methods.
    private void RoundEndedTimeout()
    {
        Logging.LogInformation("Round time ended, ending round");
        if (_areaState.IsMasterClient)
        {
            Task.Run(async () => await EndRoundAsync(Constants.DrawTeamId));
        }
    }
    
    public void EndRound()
    {
        TimerWidget.Instance.StopCountdown();

        if (!_areaState.IsMasterClient)
            return;

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;
        
        ref var room = ref areaEntity.Value.GetRoom();
        
        room.InCombatRound = false;
        foreach (var (playerId, _, mainEntity) in AllPlayers)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.GetLocalState().Pawn);
            events?.Evt_RelieveImmobilized.Invoke();
            events?.Evt_RelievePhantomRush.Invoke();
        }
    }
    
    public void ResetRoundState()
    {
        Utils.TryRunOnGameThread(TamerUtils.ClearEcsMonsters);
    }
    
    public void SetReadyState(bool isReady)
    {
        if (_playerState.LocalPlayerEntity == null)
            return;
        _playerState.LocalPlayerEntity.Value.GetState().IsReadyForPvP = isReady;
    }
    
    public void SwitchReadyState(bool isReady)
    {
        GameMessageWidget.Instance.SetThirdText(isReady ? Texts.YouAreReady : Texts.PressToSwitchTeam);
        GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(_state.AllPlayers.Count, isReady));
    }
    
    public void SwitchReadyStateMulti()
    {
        if (_areaState.InRoom && _areaState.CurrentArea is { Room: { InPvP: false, InMatchmaking: false } } && _state.AllPlayers.Count > 0)
        {
            SwitchReadyState();
        }
    }

    public void SwitchReadyStateSingle()
    {
        if (_areaState.InRoom && _areaState.CurrentArea is { Room: { InPvP: false, InMatchmaking: false } } && _state.AllPlayers.Count == 0)
        {
            SwitchReadyState();
        }
    }

    private void SwitchReadyState()
    {
        if (_playerState.LocalPlayerEntity == null)
            return;
        var isReady = _playerState.LocalPlayerEntity.Value.GetState().IsReadyForPvP;
        SetReadyState(!isReady);
        SwitchReadyState(!isReady);
    }
    
    public void SwitchTeam(bool force = false)
    {
        if (_playerState.LocalPlayerEntity == null)
            return;
        
        if (force || (_areaState.InRoom && _playerState.LocalPlayerEntity.Value.GetState().IsReadyForPvP == false && _areaState.CurrentArea is { Room: { InPvP: false, InMatchmaking: false } }))
        {
            var playerEntity = _playerState.LocalPlayerEntity;
            ref var player = ref playerEntity.Value.GetState();
            var teamId = PvPUtils.GetOppositeTeam(player.TeamId);
            player.TeamId = teamId;
        }
    }
    
    public void EnablePvP()
    {
        Logging.LogInformation("Enabled PvP");

        if (_playerState.LocalPlayerEntity == null)
            return;
        
        var playerEntity = _playerState.LocalPlayerEntity;
        ref var player = ref playerEntity.Value.GetState();
        
        var myTeam = player.TeamId;
        var otherTeams = OtherPlayers
            .Where(p => p.Player.GetState().TeamId != myTeam)
            .Select(p => p.Player.GetState().TeamId)
            .Distinct()
            .ToList();

        Logging.LogDebug("My team: {Team}", myTeam);
        Logging.LogDebug("Other teams: {Teams}", string.Join(", ", otherTeams));

        foreach (var team in Constants.AvailableTeamIds)
        {
            ClientUtils.RegisterTeamHostility(myTeam, team);
        }
    }
    
    public void DisablePvP()
    {
        Logging.LogInformation("Disabled PvP");

        if (_playerState.LocalPlayerEntity == null)
            return;
        
        var playerEntity = _playerState.LocalPlayerEntity;
        ref var player = ref playerEntity.Value.GetState();
        
        var myTeam = player.TeamId;
        var otherTeams = OtherPlayers
            .Where(p => p.Player.GetState().TeamId != myTeam)
            .Select(p => p.Player.GetState().TeamId)
            .Distinct()
            .ToList();

        Logging.LogDebug("My team: {Team}", myTeam);
        Logging.LogDebug("Other teams: {Teams}", string.Join(", ", otherTeams));

        foreach (var team in Constants.AvailableTeamIds)
        {
            ClientUtils.UnregisterTeamHostility(myTeam, team);
        }
    }
    
    public void EnterPvP()
    {
        if (!_areaState.IsMasterClient)
            return;

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No room joined.");
            return;
        }

        areaEntity.Value.GetRoom().InPvP = true;
    }
    
    public void ExitPvP()
    {
        if (!_areaState.IsMasterClient)
            return;

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No room joined.");
            return;
        }

        areaEntity.Value.GetRoom().InPvP = false;
    }
    
    public void CheckRoundEndCondition()
    {
        if (!_areaState.IsMasterClient)
            return;

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (!areaEntity.Value.GetRoom().InPvP)
            return;

        // check if all players but one are dead
        var playerEntities = AllPvPPlayers.ToList();
        var aliveTeamIds = playerEntities.Where(p => !p.Character.GetState().IsDead).Select(x => x.Player.GetState().TeamId).ToList();

        var aliveMonsters = new List<int>();
        _world.Query<HpComponent, TeamComponent>().ForEachEntity((ref HpComponent hpComp, ref TeamComponent teamComp, Entity _) =>
        {
            if (hpComp.Hp <= 0)
                return;

            aliveMonsters.Add(teamComp.TeamId);
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
            var winner = playerEntities.First(p => !p.Character.GetState().IsDead);
            _ecsLoop.Scheduler.ScheduleFunc(async (_, self, winner0) =>
            {
                await self.EndRoundAsync(winner0.Player.GetState().TeamId);
            }, this, winner);
        }
    }
    
    public bool IsSkillEnabledInPVP(int skillId)
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return true;
        
        if (skillId == Constants.ImmobilizeSkillId && !areaEntity.Value.GetRoom().ImmobilizeAllowed)
            return false;

        // more skills here
        return true;
    }
    
    private void SetOrGetRoomPropsPVP()
    {
        Logging.LogInformation("Joining or creating private room");

        if (!_areaState.IsMasterClient)
        {
            Logging.LogInformation("Not master client, skipping initialization");
            return;
        }

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No area entity found, cannot set room properties");
            return;
        }
        
        // TODO: set from initial room properties (via server allocation request)
        ref var room = ref areaEntity.Value.GetRoom();
        room.GameMode = GameMode.Private;
        room.RoundWinners = [];
        room.BotsEnabled = true; // TODO: Selector
        room.MaxPlayers = 10;
    }
    
    private int GetSmallerTeamId()
    {
        Dictionary<int, int> teamsCount = [];
        var team1Id = Constants.AvailableTeamIds[0];
        var team2Id = Constants.AvailableTeamIds[1];
        teamsCount[team1Id] = 0;
        teamsCount[team2Id] = 0;
        
        foreach (var (playerId, playerEntity, _) in AllPlayers)
        {
            if (playerId == _state.LocalPlayerId)
                continue;
            var assignedTeamId = playerEntity.GetState().TeamId;
            Logging.LogDebug("Player {PlayerId} in team {TeamId}", playerId, assignedTeamId);
            teamsCount[assignedTeamId]++;
        }

        return teamsCount[team1Id] > teamsCount[team2Id] ? team2Id : team1Id;
    }
    
    private void SetUpRoom()
    {
        if (!_areaState.IsMasterClient)
            return;
        
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        areaEntity.Value.GetRoom().InPvP = false;
    }
    
    private void SetupMatchmaking()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;
        
        ref var room = ref areaEntity.Value.GetRoom();
        if (room.GameMode == GameMode.Private)
            return;

        if (_areaState.IsMasterClient)
        {
            room.InMatchmaking = true;
            room.MatchmakingEndTime = DateTime.UtcNow.AddSeconds(Constants.MatchmakingSeconds).Ticks;
        }
    }

    private FVector GetSpawnPosition(PlayerId playerId)
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No area entity found, cannot get spawn position");
            return FVector.ZeroVector;
        }
        
        int maxPlayersCount = areaEntity.Value.GetRoom().MaxPlayers;

        float angle = playerId.RawValue / (float)maxPlayersCount * 2f * FMath.PI;
        float x = FMath.Cos(angle) * Constants.PvpStartingRadius;
        float y = FMath.Sin(angle) * Constants.PvpStartingRadius;

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        var baseLocation = levelData.PvpStartingLocation + new FVector(x, y, 0f);

        var mainEntity = _playerState.GetMainCharacterById(playerId);
        if (mainEntity == null)
        {
            Logging.LogError("Main character entity for player {PlayerId} is null", playerId);
            return FVector.ZeroVector;
        }
        
        return SpawningUtils.AdjustSpawnLocation(mainEntity.Value.GetLocalState().Pawn, baseLocation);
    }
    
    private void SetupAddedPlayer(PlayerId playerId)
    {
        var playerEntity = _playerState.GetPlayerById(playerId);
        if (playerEntity == null)
        {
            Logging.LogError("Player entity is null");
            return;
        }
        
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No area entity found, cannot setup added player");
            return;
        }

        ref var room = ref areaEntity.Value.GetRoom();
        ref var player = ref playerEntity.Value.GetState();
        
        // set IsSpectator if client should be (joining during fight)
        var isSpectator = player.IsSpectator;

        if (!isSpectator)
        {
            player.IsSpectator = room.InPvP && !player.IsReadyForPvP;
        }

        // readiness callback
        if (player.IsReadyForPvP)
        {
            NotifyPlayerReadinessChanged(player.NickName, player.IsReadyForPvP);
        }

        if (_state.AllPlayers.Count() == room.MaxPlayers)
        {
            EndMatchmaking();
        }
    }
    
    private void EndMatchmaking()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No area entity found, cannot end matchmaking");
            return;
        }
        
        if (_areaState.IsMasterClient)
        {
            areaEntity.Value.GetRoom().InMatchmaking = false;
            _rpc.SendEndMatchmaking();
        }

        TimerWidget.Instance.StopCountdown();
    }
    
    public void NotifyPlayerReadinessChanged(string playerNickname, bool isReady)
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        var readyCount = AllPlayers.Count(x => x.Player.GetState().IsReadyForPvP);

        if (_areaState.IsMasterClient) // send this only once
        {
            if (isReady)
            {
                _chatter.SendServerMessage("PlayerIsReady", playerNickname);
            }
            else
            {
                _chatter.SendServerMessage("PlayerIsNotReady", playerNickname);
            }
        }
        
        if (isReady)
        {
            if ((OtherPlayerIds.Any() || _areaState.CurrentArea?.GetRoom().BotsEnabled == true) && readyCount == _state.AllPlayers.Count)
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
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;
        
        ref var room = ref areaEntity.Value.GetRoom();
        
        PvPUtils.IsAfterLoadingScreen = true;
        if (room.InMatchmaking)
        {
            var timeDifference = new DateTime(room.MatchmakingEndTime, DateTimeKind.Utc) - DateTime.UtcNow;
            TimerWidget.Instance.StartCountdown(0, timeDifference.Seconds, EndMatchmaking);
            PvPUtils.SetupMatchmakingUi();
        }
        else if (_playerState.LocalPlayerEntity?.GetState().IsSpectator == false)
        {
            PvPUtils.SetupLobbyUi();
        }
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        SetOrGetRoomPropsPVP();

        Logging.LogInformation("Joined room");

        SetUpRoom();
        LobbyStatusWidget.Instance.SetReadyCount(OtherPlayers.Count(x => x.Player.GetState().IsReadyForPvP));
        SetupMatchmaking();

        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        var spawnPosition = GetSpawnPosition(player.PlayerId);
        var data = new PlayerTransformData(player.PlayerId, spawnPosition, FRotator.ZeroRotator);
        _rpc.OnBroadcastPlayerTransform(data);
    }
    
    private void OnOtherPlayerInsideAreaHandler(PlayerId playerId, AreaId areaId, OtherPlayerInsideAreaReason arg3)
    {
        Logging.LogInformation("Player {PlayerId} entered the room", playerId);
        SetupAddedPlayer(playerId);
    }
    
    private void OnOtherPlayerOutsideAreaHandler(PlayerId playerId, AreaId areaId, OtherPlayerOutsideAreaReason arg3)
    {
        if (_areaState.IsMasterClient)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(Constants.PlayerTtlMs);
                CheckRoundEndCondition();
            });
        }
    }

    private bool? lastReady;
    
    private void OnUpdateLoopHandler(CommandBufferSynced cb)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        
        var playerComp = playerEntity.Value.GetState();
        if (lastReady == playerComp.IsReadyForPvP)
            return;

        var targetPlayerNickname = playerComp.NickName;
        NotifyPlayerReadinessChanged(targetPlayerNickname, playerComp.IsReadyForPvP);
        lastReady = playerComp.IsReadyForPvP;
    }
    
    #endregion
    
    #region RPC
    
    public void SendPvPEvent(PvPEvent ev, int data = 0)
    {
        if (!_areaState.IsMasterClient)
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

        Logging.LogInformation("Received PvP event: {Event}", ev);

        switch (ev)
        {
            case PvPEvent.RoundStart:
            {
                _ecsLoop.Scheduler.Schedule(_ => PvPUtils.ShowPvPCountDown());
                StartRound();
                EnablePvP();
                EnterPvP();
                break;
            }
            case PvPEvent.RoundEnd:
            {
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

                var playerEntity = _playerState.LocalPlayerEntity;
                if (playerEntity == null)
                    return;
                
                if (winnerTeamId == playerEntity.Value.GetState().TeamId)
                {
                    AssetUtils.PlayBossDefeatedSound();
                }

                break;
            }
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

                // ReSharper disable once AsyncVoidMethod
                _ecsLoop.Scheduler.Schedule(async void (_, self) =>
                {
                    if (self._areaState.IsMasterClient)
                    {
                        foreach (var d in self.SpectatingPlayers)
                        {
                            d.Player.GetState().IsSpectator = false;
                        }
                    }

                    await Task.Delay(2000);
                    PvPUtils.EndTournament();
                    self.ExitPvP();
                    self.SetReadyState(false);
                }, this);

                break;
            }
            case PvPEvent.ResetStats:
            {
                ResetRoundState();
                
                var mainEntity = _playerState.LocalMainCharacter;
                if (mainEntity == null)
                    return;
                
                if (!mainEntity.Value.GetState().IsDead)
                {
                    Utils.TryRunOnGameThread(() =>
                    {
                        TamerUtils.DestroyAllTamers();
                        var events = BUS_EventCollectionCS.Get(mainEntity.Value.GetLocalState().Pawn!);

                        if (events == null)
                        {
                            Logging.LogError("events are null");
                            return;
                        }

                        events.Evt_TriggerTeleportResetPlayer!.Invoke();
                    });
                }

                if (_areaState.IsMasterClient)
                {
                    // reset other players' Hp to HpMax if they are not dead
                    foreach (var d in OtherPlayers)
                    {
                        ref var otherMain = ref d.Character.GetState();
                        ref var otherLocalMain = ref d.Character.GetLocalState();
                        
                        if (!otherMain.IsDead)
                        {
                            if (otherLocalMain.Pawn == null)
                            {
                                Logging.LogError("Pawn is null in {Patch}", nameof(OnPvpEvent));
                                return;
                            }

                            var attrContainer = (BUC_AttrContainer?)BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(otherLocalMain.Pawn);
                            if (attrContainer != null)
                            {
                                var hpMax = attrContainer.GetFloatValue(EBGUAttrFloat.HpMax);
                                attrContainer.SetFloatValue(EBGUAttrFloat.Hp, hpMax);
                                otherMain.Hp = hpMax;
                            }
                        }
                    }
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(ev));
        }
    }

    #endregion
}