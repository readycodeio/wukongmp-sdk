using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using b1;
using CSharpModBase;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Helpers;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Resources;
using WukongMp.PvP.UI;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP.Gamemode;

internal partial class PvpMode : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;

    public bool IsRoundEnding { get; private set; }
    private readonly Store _world;
    private readonly ClientState _state;
    private readonly WukongAreaState _areaState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongPlayerPawnState _playerPawnState;
    private readonly WukongEventBus _eventBus;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongChatter _chatter;
    private readonly GameplayEventRouter _eventRouter;
    private readonly ClientOwnershipManager _clientOwnership;
    private readonly WukongPawnState _pawnState;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly PvpWidgetManager _pvpWidgetManager;
    private readonly ILogger _logger;

    public int PendingDaShengSecondPhaseSpawns { get; private set; }
    private readonly HashSet<NetworkId> SpawnedDaSheng2 = [];

    private readonly CountdownTimer _countdownTimer = new(1, 5);

    private (PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)? GetEntities(PlayerId playerId)
    {
        var playerEntity = _playerState.GetPlayerById(playerId);
        var mainEntity = _playerState.GetMainCharacterById(playerId);
        if (!playerEntity.HasValue || !mainEntity.HasValue)
            return null;
        return (PlayerId: playerId, Player: playerEntity.Value, Character: mainEntity.Value);
    }

    public IEnumerable<PlayerId> SpectatingPlayerIds
        => _state.AreaPlayers.Where(p => _playerState.GetMainCharacterById(p)?.GetPvP().IsSpectator == true);

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> SpectatingPlayers
        => SpectatingPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<PlayerId> AllPvPPlayerIds
        => _state.AreaPlayers.Where(p => _playerState.GetMainCharacterById(p)?.GetPvP().IsSpectator == false);

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> AllPvPPlayers
        => AllPvPPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> AllPlayers
        => _state.AreaPlayers.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> OtherPlayers
        => _state.OtherAreaPlayers.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public PvpMode(
        Store world,
        RelaySerializer serializer,
        IRelayClient relayClient,
        ClientState state,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongPlayerPawnState playerPawnState,
        WukongEventBus eventBus,
        WukongRpcCallbacks rpc,
        WukongChatter chatter,
        GameplayEventRouter eventRouter,
        ClientOwnershipManager clientOwnership,
        WukongPawnState pawnState,
        IClientEcsUpdateLoop ecsLoop,
        FreeCameraManager freeCameraManager,
        PvpWidgetManager pvpWidgetManager,
        ILogger logger
    )
    {
        _world = world;
        Serializer = serializer;
        RelayClient = relayClient;
        _state = state;
        _areaState = areaState;
        _playerState = playerState;
        _playerPawnState = playerPawnState;
        _eventBus = eventBus;
        _rpc = rpc;
        _chatter = chatter;
        _eventRouter = eventRouter;
        _clientOwnership = clientOwnership;
        _pawnState = pawnState;
        _ecsLoop = ecsLoop;
        _pvpWidgetManager = pvpWidgetManager;
        _logger = logger;

        _eventBus.OnBeginPlayGameplayLevel += OnBeginPlayGameplayLevel;

        _state.OnJoinedArea += OnJoinedAreaHandler;
        _state.OnOtherPlayerInsideArea += OnOtherPlayerInsideAreaHandler;

        _eventRouter.OnUnitDead += OnUnitDead;
        _eventRouter.OnMonsterSpawned += OnMonsterSpawned;
        _eventRouter.OnLanguageChanged += OnLanguageChanged;

        _playerPawnState.OnPlayerPawnSpawned += OnPlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized += OnMainCharacterEntityInitialized;
        _rpc.OnPvpEventReceived += OnPvpEvent;
    }

    private void OnLanguageChanged(CultureInfo culture)
    {
        PvpTexts.Culture = culture;
    }

    public void Dispose()
    {
        _state.OnOtherPlayerInsideArea -= OnOtherPlayerInsideAreaHandler;
        _state.OnJoinedArea -= OnJoinedAreaHandler;

        _eventBus.OnBeginPlayGameplayLevel -= OnBeginPlayGameplayLevel;

        _eventRouter.OnUnitDead -= OnUnitDead;
        _eventRouter.OnMonsterSpawned -= OnMonsterSpawned;
        _eventRouter.OnLanguageChanged -= OnLanguageChanged;

        _playerPawnState.OnPlayerPawnSpawned -= OnPlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized -= OnMainCharacterEntityInitialized;
        _rpc.OnPvpEventReceived -= OnPvpEvent;
    }

    private void OnMonsterSpawned(Entity entity)
    {
        var teamComp = entity.GetComponent<TeamComponent>();
        var tamerComp = entity.GetComponent<TamerComponent>();
        var localTamerComp = entity.GetComponent<LocalTamerComponent>();

        var teamColor = PvpUtils.GetTeamColorString(teamComp.TeamId);
        MarkerUtils.CreateMarkerForCharacter(new TamerEntity(entity), teamColor);
        if (tamerComp.UnitPath == UnitPathsConfig.GetUnitPath(CharacterKind.Monkey))
        {
            SpawningUtils.SetMonkeyBotConfig(localTamerComp.Tamer!.GetMonster());
        }
    }

    private void OnPlayerPawnSpawned(MainCharacterEntity mainCharacterEntity, BGUCharacterCS pawn)
    {
        var teamColor = PvpUtils.GetTeamColorString(mainCharacterEntity.GetTeam().TeamId);
        var marker = MarkerUtils.CreateMarkerForCharacter(mainCharacterEntity, teamColor); // 3D marker above player
        if (marker == null)
        {
            _logger.LogError("Failed to create marker for player {PlayerId}.", mainCharacterEntity.GetState().CharacterNickName);
        }
    }

    private void OnMainCharacterEntityInitialized(MainCharacterEntity mainCharacterEntity)
    {
        var spawnPosition = PvpUtils.GetSpawnPosition(GameUtils.GetControlledPawn(), mainCharacterEntity.GetState().PlayerId.RawValue, Constants.MaxPlayers);
        PlayerUtils.TeleportLocalPlayer(mainCharacterEntity, spawnPosition, FRotator.ZeroRotator, false);

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity != null && _areaState.PvpState.HasValue)
        {
            ref var pvpComp = ref mainCharacterEntity.GetPvP();

            // Set IsSpectator if joining during fight.
            pvpComp.IsSpectator = _areaState.PvpState.Value.InPvP;
            Logging.LogDebug("Setting IsSpectator to {IsSpectator}", pvpComp.IsSpectator);
        }
    }

    public void StartPvP()
    {
        if (_areaState.OwnsPvpState)
        {
            _areaState.OwnedPvpStateRef().RoundWinners = [];
            Task.Run(StartRoundAsync);
        }
    }

    public async Task StartRoundAsync()
    {
        if (!_areaState.OwnsPvpState)
        {
            return;
        }

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        PlacePlayers(levelData.PvpStartingLocation, levelData.PvpRadius);
        await Task.Delay(100);

        SendPvPEvent(PvpEvent.RoundStart);
    }

    private void PlacePlayers(FVector center, float radius)
    {
        if (!_areaState.OwnsPvpState)
        {
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

        foreach (var (playerId, playerEntity, mainEntity) in playerEntities)
        {
            ref var localMainComp = ref mainEntity.GetLocalState();
            var team = playerEntity.GetState().TeamId;

            var teamBaseAngle = teamIndex[team] * teamAngleStep;
            var memberIndex = teamMemberIndex[team];

            var angle = teamBaseAngle + (memberIndex + 1) * entityOffsetAngle;
            var x = center.X + radius * MathF.Cos(angle);
            var y = center.Y + radius * MathF.Sin(angle);

            teamMemberIndex[team]++;
            var newPlayerLocation = PvpUtils.AdjustSpawnLocation(localMainComp.Pawn, new FVector(x, y, center.Z));
            var payload = new PlayerTransformData(playerId, newPlayerLocation, UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0, 0, 500)));
            _rpc.SendBroadcastPlayerTransform(payload);
        }
    }

    public async Task EndRoundAsync(int winner)
    {
        if (IsRoundEnding)
            return;

        if (!_areaState.OwnsPvpState)
        {
            return;
        }

        ref var pvpState = ref _areaState.OwnedPvpStateRef();

        IsRoundEnding = true;

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("Current area is null, cannot end round");
            return;
        }

        // disable pvp until next round
        SendPvPEvent(PvpEvent.RoundEnd, winner);

        // increment round number
        pvpState.SetLastRoundWinnerTeam(winner);

        // wait until all players death animations are finished
        await Task.Delay(5000);

        if (!_areaState.OwnsPvpState)
        {
            Logging.LogDebug("Master client disconnected before finishing EndRoundAsync");
            return;
        }

        await ResetHpAndRespawnAllPlayers();

        // resolve tournament
        var winnersSoFar = _areaState.PvpState!.Value.RoundWinners.ToList();
        var winnersByTeam = winnersSoFar.Where(w => w != Constants.DrawTeamId).GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

        // check if only one team is present
        if (AllPvPPlayers.Select(p => p.Player.GetState().TeamId).Distinct().Count() == 1)
        {
            SendPvPEvent(PvpEvent.TournamentEnd, winner);
            IsRoundEnding = false;
            return;
        }

        // check if any team won more than half of the rounds
        var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > areaEntity.Value.GetRoom().TournamentRounds / 2);
        if (winnerTeam.Key != 0)
        {
            SendPvPEvent(PvpEvent.TournamentEnd, winnerTeam.Key);
            IsRoundEnding = false;
            return;
        }

        // otherwise, check if we have a tie
        if (_areaState.PvpState.Value.CurrentRound > areaEntity.Value.GetRoom().TournamentRounds)
        {
            if (winnersByTeam.Count > 0)
            {
                // if any team have won more than others
                int maxWins = winnersByTeam.Values.Max();
                var winningTeams = winnersByTeam.Where(t => t.Value == maxWins).Select(t => t.Key).ToList();
                if (winningTeams.Count == 1)
                {
                    SendPvPEvent(PvpEvent.TournamentEnd, winningTeams[0]);
                }
                else
                {
                    SendPvPEvent(PvpEvent.TournamentEnd, Constants.DrawTeamId);
                }
            }
            else
            {
                // that was the final round
                SendPvPEvent(PvpEvent.TournamentEnd, Constants.DrawTeamId);
            }
        }
        else
        {
            // start next round
            await StartRoundAsync();
        }

        IsRoundEnding = false;
    }

    private async Task ResetHpAndRespawnAllPlayers()
    {
        if (!_areaState.OwnsPvpState)
        {
            return;
        }

        // resurrect dead players and restore health to living ones
        SendPvPEvent(PvpEvent.ResetStats);
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
        ClearLoobyCountdown();
        _pvpWidgetManager.StartRound();

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        if (!_areaState.OwnsPvpState)
            return;

        _areaState.OwnedPvpStateRef().InPvP = true;

        var monsterCount = 0;
        _world.Query<LocalTamerComponent>().ForEachEntity((ref localTamerComp, _) =>
        {
            if (localTamerComp.IsTamerSynced)
            {
                monsterCount++;
            }
        });

        if (!OtherPlayers.Any() && monsterCount == 0)
        {
            // FIXME: Is there any way to get rid of having those checks all over the place? Seems very tedious,
            // and it handles a fringe case where somehow the player got disconnected.
            if (_playerState.LocalPlayerEntity != null)
            {
                var teamId = _playerState.LocalPlayerEntity.Value.GetState().TeamId;
                var oppositeId = PvpUtils.GetOppositeTeam(teamId);
                _ecsLoop.Scheduler.Schedule(static (_, oppositeId0) => { PvpUtils.SpawnBots(oppositeId0); }, oppositeId);
            }
        }
    }

    public void EndRound()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        if (_areaState.OwnsPvpState)
            _areaState.OwnedPvpStateRef().InPvP = false;

        if (_areaState.IsMasterClient)
        {
            foreach (var (_, _, mainEntity) in AllPlayers)
            {
                var events = BUS_EventCollectionCS.Get(mainEntity.GetLocalState().Pawn);
                events?.Evt_RelieveImmobilized.Invoke();
                events?.Evt_RelievePhantomRush.Invoke();
            }
        }
    }

    public void ResetRoundState()
    {
        _ecsLoop.Scheduler.Schedule(_ => { TamerUtils.DestroyAllTamers(); });
    }

    public void SetReadyState(bool isReady)
    {
        if (_playerState.LocalMainCharacter == null)
            return;
        _playerState.LocalMainCharacter.Value.GetPvP().IsReadyForPvP = isReady;
    }

    public void SwitchReadyStateMulti()
    {
        if (_areaState is { InRoom: true, PvpState.InPvP: false } && _state.AllPlayers.Count > 0 && _playerState.LocalMainCharacter?.GetPvP().IsSpectator is not true)
        {
            SwitchReadyState();
        }
    }

    private void SwitchReadyState()
    {
        if (_playerState.LocalMainCharacter == null)
            return;
        var newIsReady = !_playerState.LocalMainCharacter.Value.GetPvP().IsReadyForPvP;
        var nickname = _playerState.LocalMainCharacter.Value.GetState().CharacterNickName;
        SetReadyState(newIsReady);
        _pvpWidgetManager.SwitchReadyState(newIsReady);
        _chatter.SendServerMessage(newIsReady ? "PlayerIsReady" : "PlayerIsNotReady", nickname);
    }

    public void SwitchTeam(bool force = false)
    {
        if (_playerState.LocalMainCharacter == null || _playerState.LocalPlayerEntity == null)
            return;

        if (force || _areaState.InRoom && !_playerState.LocalMainCharacter.Value.GetPvP().IsReadyForPvP && _areaState.PvpState is { InPvP: false })
        {
            var playerEntity = _playerState.LocalPlayerEntity;
            ref var player = ref playerEntity.Value.GetState();
            var teamId = PvpUtils.GetOppositeTeam(player.TeamId);
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
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (_areaState.OwnsPvpState)
            _areaState.OwnedPvpStateRef().InPvP = true;
    }

    public void ExitPvP()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (_areaState.OwnsPvpState)
            _areaState.OwnedPvpStateRef().InPvP = false;
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
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        if (_areaState.OwnsPvpState)
            _areaState.OwnedPvpStateRef().InPvP = false;
    }

    [Obsolete("Matchmaking is not supported for now")]
    private void EndMatchmaking()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No area entity found, cannot end matchmaking");
            return;
        }
    }

    public void StartLobbyCountdown(int seconds)
    {
        _pvpWidgetManager.SetMainMessage(Texts.StartingGame);
        _pvpWidgetManager.UpdateRoundCountdown(0, seconds);
        _pvpWidgetManager.ShowCountdown();

        _countdownTimer.SetTime(0, seconds);
        _countdownTimer.Start(() =>
        {
            ClearLoobyCountdown();
            StartPvP();
        }, _pvpWidgetManager.UpdateRoundCountdown);
    }

    public void CancelLobbyCountdown()
    {
        ClearLoobyCountdown();
        _pvpWidgetManager.SetMainMessage(Texts.InMultiplayer);
    }

    public void ClearLoobyCountdown()
    {
        _countdownTimer.Reset();
        _pvpWidgetManager.HideCountdown();
    }

    #region Event Handlers

    private void OnBeginPlayGameplayLevel()
    {
        TamerUtils.DestroyAllTamers();
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        Logging.LogInformation("Joined room");

        SetUpRoom();
        _pvpWidgetManager.UpdateReadyCount(OtherPlayers.Count(x => x.Character.GetPvP().IsReadyForPvP));

        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        player.TeamId = GetSmallerTeamId();
        Logging.LogDebug("Assigned team {Id} for player", player.TeamId);
    }

    private void OnOtherPlayerInsideAreaHandler(PlayerId playerId, AreaId areaId, OtherPlayerInsideAreaReason arg3)
    {
        Logging.LogInformation("Player {PlayerId} entered the room", playerId);
        if (_state.AreaPlayers.Count == Constants.MaxPlayers)
        {
            EndMatchmaking();
        }
    }

    private void OnUnitDead(Entity victim, Entity attacker)
    {
        if (_areaState is { PvpState.InPvP: true })
        {
            if (_pawnState.TryGetTamerEntity(victim, out var victimTamerEntity))
            {
                if (!_clientOwnership.OwnsEntity(victimTamerEntity.Value.Entity))
                    return;

                ref var localTamer = ref victimTamerEntity.Value.GetLocalTamer();
                var tamerClass = localTamer.Tamer?.GetClass();
                var netId = victimTamerEntity.Value.GetMeta().NetId;
                var character = localTamer.Pawn;
                if (character != null && tamerClass != null && tamerClass.PathName == UnitPathsConfig.GetUnitPath(CharacterKind.DaSheng))
                {
                    var teamId = character.GetTeamIDInCS();
                    var location = character.GetActorLocation();

                    if (SpawnedDaSheng2.Add(netId))
                    {
                        PendingDaShengSecondPhaseSpawns++;
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(5000);
                            Utils.TryRunOnGameThread(() =>
                            {
                                SpawningUtils.SpawnUnitAsOwner(CharacterKind.DaSheng2, location, teamId);
                                PendingDaShengSecondPhaseSpawns--;
                            });
                        });
                    }
                    else
                    {
                        Logging.LogDebug("Would spawn DaSheng2, but already spawned for this monster: {Monster}", netId);
                    }
                }
            }
        }
    }

    #endregion

    #region RPC

    public void SendPvPEvent(PvpEvent ev, int data = 0)
    {
        if (!_areaState.OwnsPvpState)
        {
            Logging.LogError("Only room owner can send start countdown.");
            return;
        }

        Logging.LogInformation("Sending PvP event: {Event}", ev);

        _rpc.SendPvpEvent([(int)ev, data]);
    }

    internal void OnPvpEvent(int[] data)
    {
        var ev = (PvpEvent)data[0];
        var winnerTeamId = data[1];

        Logging.LogInformation("Received PvP event: {Event}", ev);

        switch (ev)
        {
            case PvpEvent.RoundStart:
            {
                _ecsLoop.Scheduler.Schedule(_ => PvpUtils.ShowPvPCountDown());
                StartRound();
                EnablePvP();
                EnterPvP();
                break;
            }
            case PvpEvent.RoundEnd:
            {
                DisablePvP();
                EndRound();

                if (winnerTeamId == Constants.DrawTeamId)
                {
                    UiUtils.ShowTip(Texts.RoundDraw, true);
                }
                else
                {
                    UiUtils.ShowTip(string.Format(Texts.RoundEndedWinner, PvpUtils.GetLocalizedTeamName(winnerTeamId)), true);
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
            case PvpEvent.TournamentEnd:
            {
                if (winnerTeamId == Constants.DrawTeamId)
                {
                    UiUtils.ShowTip(Texts.TournamentDraw, true);
                }
                else
                {
                    UiUtils.ShowTip(string.Format(Texts.TournamentEndedWinner, PvpUtils.GetLocalizedTeamName(winnerTeamId)), true);
                }

                // ReSharper disable once AsyncVoidMethod
                _ecsLoop.Scheduler.Schedule(async static void (_, self) =>
                {
                    if (self._playerState.LocalMainCharacter.HasValue)
                        self._playerState.LocalMainCharacter.Value.GetPvP().IsSpectator = false;
                    await Task.Delay(2000);
                    Logging.LogInformation("End tournament");
                    self._pvpWidgetManager.SetupLobbyUi();
                    self.ExitPvP();
                    self.SetReadyState(false);
                }, this);

                break;
            }
            case PvpEvent.ResetStats:
            {
                ResetRoundState();

                var mainEntity = _playerState.LocalMainCharacter;
                if (mainEntity == null)
                    return;

                if (!mainEntity.Value.GetState().IsDead)
                {
                    _ecsLoop.Scheduler.Schedule(static (_, mainEntity0) =>
                    {
                        var events = BUS_EventCollectionCS.Get(mainEntity0.GetLocalState().Pawn!);
                        events?.Evt_TriggerTeleportResetPlayer!.Invoke();
                    }, mainEntity.Value);
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(ev));
        }
    }

    #endregion
}