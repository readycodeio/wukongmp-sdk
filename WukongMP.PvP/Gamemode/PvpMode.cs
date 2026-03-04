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
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.Helpers;
using WukongMp.Api.Mapping;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.GameEvents;
using WukongMp.PvP.Resources;
using WukongMp.PvP.UI;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP.GameMode;

internal partial class PvpMode : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;

    public bool IsRoundEnding { get; private set; }
    private readonly Store _world;
    private readonly MappedEventManager _mappedEvent;
    private readonly ClientState _state;
    private readonly WukongAreaState _areaState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongPlayerPawnState _playerPawnState;
    private readonly WukongEventBus _eventBus;
    private readonly WukongClientRpcCallbacks _clientRpc;
    private readonly WukongChatter _chatter;
    private readonly GameplayEventRouter _eventRouter;
    private readonly WukongMappingPolicyDirectory _policyDir;
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
        var mainEntity = _playerState.GetMainCharacterByPlayerId(playerId);
        if (!playerEntity.HasValue || !mainEntity.HasValue)
            return null;
        return (PlayerId: playerId, Player: playerEntity.Value, Character: mainEntity.Value);
    }

    private bool GetPvPPlayerIds(PlayerId playerId)
    {
        var playerEntity = _playerState.GetMainCharacterByPlayerId(playerId);
        if (playerEntity.HasValue)
        {
            ref var pvpComp = ref playerEntity.Value.GetPvP();
            return !pvpComp.IsObserver;
        }

        return false;
    }

    public IEnumerable<PlayerId> SpectatingPlayerIds
        => _state.AreaPlayers.Where(p => _playerState.GetMainCharacterByPlayerId(p)?.GetPvP().IsSpectator == true);

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> SpectatingPlayers
        => SpectatingPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<PlayerId> AllPvPPlayerIds
        => _state.AreaPlayers.Where(GetPvPPlayerIds);

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> AllPvPPlayers
        => AllPvPPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> AllPlayers
        => _state.AreaPlayers.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> OtherPlayers
        => _state.OtherAreaPlayers.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

    public PvpMode(
        Store world,
        MappedEventManager mappedEvent,
        RelaySerializer serializer,
        IRelayClient relayClient,
        ClientState state,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongPlayerPawnState playerPawnState,
        WukongEventBus eventBus,
        WukongClientRpcCallbacks clientRpc,
        WukongChatter chatter,
        GameplayEventRouter eventRouter,
        WukongMappingPolicyDirectory policyDir,
        ClientOwnershipManager clientOwnership,
        WukongPawnState pawnState,
        IClientEcsUpdateLoop ecsLoop,
        PvpWidgetManager pvpWidgetManager,
        ILogger logger
    )
    {
        _world = world;
        _mappedEvent = mappedEvent;
        Serializer = serializer;
        RelayClient = relayClient;
        _state = state;
        _areaState = areaState;
        _playerState = playerState;
        _playerPawnState = playerPawnState;
        _eventBus = eventBus;
        _clientRpc = clientRpc;
        _chatter = chatter;
        _eventRouter = eventRouter;
        _policyDir = policyDir;
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
        _eventRouter.OnPlayerChangedTeam += OnPlayerChangedTeam;
        _eventRouter.OnLocalPlayerChangedSpectator += OnLocalPlayerChangedSpectator;

        _playerPawnState.OnPlayerPawnSpawned += OnPlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized += OnMainCharacterEntityInitialized;
        _clientRpc.OnPvpEventReceived += OnPvpEvent;
    }

    public void Dispose()
    {
        _state.OnOtherPlayerInsideArea -= OnOtherPlayerInsideAreaHandler;
        _state.OnJoinedArea -= OnJoinedAreaHandler;

        _eventBus.OnBeginPlayGameplayLevel -= OnBeginPlayGameplayLevel;

        _eventRouter.OnUnitDead -= OnUnitDead;
        _eventRouter.OnMonsterSpawned -= OnMonsterSpawned;
        _eventRouter.OnLanguageChanged -= OnLanguageChanged;
        _eventRouter.OnPlayerChangedTeam -= OnPlayerChangedTeam;
        _eventRouter.OnLocalPlayerChangedSpectator -= OnLocalPlayerChangedSpectator;

        _playerPawnState.OnPlayerPawnSpawned -= OnPlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized -= OnMainCharacterEntityInitialized;
        _clientRpc.OnPvpEventReceived -= OnPvpEvent;
    }

    private void OnPlayerChangedTeam(PlayerEntity player, MainCharacterEntity character)
    {
        ref var mainComp = ref character.GetState();
        ref var localMainComp = ref character.GetLocalState();
        var teamComp = character.GetTeam();

        Logging.LogDebug("Updating player {Nickname} marker to team {Team}", mainComp.CharacterNickName, teamComp.TeamId);
        if (localMainComp.MarkerActor != null)
        {
            var teamColor = PvpUtils.GetTeamColorString(teamComp.TeamId);
            localMainComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {mainComp.CharacterNickName} {teamColor}", true);
        }
    }

    private void OnLocalPlayerChangedSpectator(bool enabled)
    {
        if (_playerState.LocalMainCharacter == null || _playerState.LocalPlayerEntity == null || !_areaState.InRoom)
            return;

        ref var player = ref _playerState.LocalPlayerEntity.Value.GetState();
        if (enabled && _playerState.LocalMainCharacter.Value.GetPvP().IsObserver)
        {
            player.TeamId = PvpConstants.SpectatorTeamId;
        }
        else if (!enabled && player.TeamId == PvpConstants.SpectatorTeamId)
        {
            player.TeamId = GetSmallerTeamId();
        }
    }

    private void OnLanguageChanged(CultureInfo culture)
    {
        PvpTexts.Culture = culture;
    }

    private void OnMonsterSpawned(Entity entity)
    {
        var teamComp = entity.GetComponent<TeamComponent>();
        var teamColor = PvpUtils.GetTeamColorString(teamComp.TeamId);
        MarkerUtils.CreateMarkerForCharacter(new TamerEntity(entity), teamColor);
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
            if (_areaState.PvpState.Value.InPvP)
            {
                PlayerUtils.EnableSpectator(mainCharacterEntity, SpectatorReason.Observer);
            }
        }

        PlayerUtils.SetLocalPlayerDamageImmunity(mainCharacterEntity, true);
    }

    private void StartPvP()
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

        PlacePlayers();
        await Task.Delay(100);

        _mappedEvent.InvokeInGameAndNotifyEcs(new PvpEvent(PvpEventKind.RoundStart));
    }

    private void PlacePlayers()
    {
        if (!_areaState.OwnsPvpState)
        {
            return;
        }

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        var center = levelData.PvpStartingLocation;
        var radius = levelData.PvpRadius;
        var customPositions = levelData.CustomTeamSpawns;

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

        foreach (var (_, playerEntity, mainEntity) in playerEntities)
        {
            var team = playerEntity.GetState().TeamId;
            var memberIndex = teamMemberIndex[team];
            var teamBaseAngle = teamIndex[team] * teamAngleStep;

            FVector spawnLocation;
            float teamAngleOffset = 0f;

            if (customPositions != null && customPositions.TryGetSpawnPosition(team, out var teamSpawn))
            {
                var dir = teamSpawn - center;
                var customTeamAngle = MathF.Atan2(dir.Y, dir.X);
                teamAngleOffset = customTeamAngle - teamBaseAngle;

                var angle = customTeamAngle + memberIndex * entityOffsetAngle;
                var x = center.X + radius * MathF.Cos(angle);
                var y = center.Y + radius * MathF.Sin(angle);
                spawnLocation = new FVector(x, y, center.Z);
            }
            else
            {
                var angle = teamBaseAngle + teamAngleOffset + memberIndex * entityOffsetAngle;
                var x = center.X + radius * MathF.Cos(angle);
                var y = center.Y + radius * MathF.Sin(angle);
                spawnLocation = new FVector(x, y, center.Z);
            }

            teamMemberIndex[team]++;
            var newPlayerLocation = PvpUtils.AdjustSpawnLocation(mainEntity.Pawn, spawnLocation);
            _mappedEvent.InvokeInGameAndNotifyEcs(new BroadcastPlayerTransformEvent(
                entity: mainEntity.Entity,
                location: newPlayerLocation,
                rotation: UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0, 0, 500))
            ));
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
        _mappedEvent.NotifyEcs(new PvpEvent(PvpEventKind.RoundEnd, winner)); // TODO: policy for owning the PvP state

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
        var winnersByTeam = winnersSoFar.Where(w => w != PvpConstants.DrawTeamId).GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

        // check if only one team is present
        if (AllPvPPlayers.Select(p => p.Player.GetState().TeamId).Distinct().Count() == 1)
        {
            _mappedEvent.NotifyEcs(new PvpEvent(PvpEventKind.TournamentEnd, winner));
            IsRoundEnding = false;
            return;
        }

        // check if any team won more than half of the rounds
        var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > areaEntity.Value.GetRoom().TournamentRounds / 2);
        if (winnerTeam.Key != 0)
        {
            _mappedEvent.NotifyEcs(new PvpEvent(PvpEventKind.TournamentEnd, winnerTeam.Key));
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
                    _mappedEvent.NotifyEcs(new PvpEvent(PvpEventKind.TournamentEnd, winningTeams[0]));
                }
                else
                {
                    _mappedEvent.NotifyEcs(new PvpEvent(PvpEventKind.TournamentEnd, PvpConstants.DrawTeamId));
                }
            }
            else
            {
                // that was the final round
                _mappedEvent.NotifyEcs(new PvpEvent(PvpEventKind.TournamentEnd, PvpConstants.DrawTeamId));
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
        _mappedEvent.NotifyEcs(new PvpEvent(PvpEventKind.ResetStats));

        foreach (var (_, _, mainEntity) in AllPlayers)
        {
            if (mainEntity.GetState().IsDead)
            {
                ref var metaComp = ref mainEntity.GetMeta();

                _clientRpc.SendRebirthPlayer(metaComp.NetId, false);
            }
        }

        // wait for that to finish
        await Task.Delay(6500);
    }

    private void StartRound()
    {
        ClearLoobyCountdown();
        _pvpWidgetManager.StartRound();

        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        if (!_areaState.OwnsPvpState)
            return;

        _areaState.OwnedPvpStateRef().InPvP = true;
    }

    private void EndRound()
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
                var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
                events?.Evt_RelieveImmobilized.Invoke();
                events?.Evt_RelievePhantomRush.Invoke();
            }
        }
    }

    private void ResetRoundState()
    {
        _ecsLoop.Scheduler.Schedule(_ => { DestroyTamersOnArena(); });
    }

    private void SetReadyState(bool isReady)
    {
        if (_playerState.LocalMainCharacter == null)
            return;
        _playerState.LocalMainCharacter.Value.GetPvP().IsReadyForPvP = isReady;
    }

    public void SwitchReadyStateMulti()
    {
        if (_areaState is { InRoom: true, PvpState.InTournament: false } && _state.AllPlayers.Count > 0 && _playerState.LocalMainCharacter?.GetPvP().IsSpectator is not true)
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

        if (force || _areaState.InRoom && !_playerState.LocalMainCharacter.Value.GetPvP().IsReadyForPvP && _areaState.PvpState is { InTournament: false } && !_playerState.LocalMainCharacter.Value.GetPvP().IsSpectator)
        {
            var playerEntity = _playerState.LocalPlayerEntity;
            ref var player = ref playerEntity.Value.GetState();
            var teamId = PvpUtils.GetOppositeTeam(player.TeamId);
            player.TeamId = teamId;
        }
    }

    private void EnablePvP()
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

        foreach (var team1 in PvpConstants.AllTeamIds)
        {
            foreach (var team2 in PvpConstants.AllTeamIds)
            {
                ClientUtils.RegisterTeamHostility(team1, team2);
            }
        }
    }

    private void DisablePvP()
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


        foreach (var team1 in PvpConstants.AllTeamIds)
        {
            foreach (var team2 in PvpConstants.AllTeamIds)
            {
                ClientUtils.UnregisterTeamHostility(team1, team2);
            }
        }
    }

    private void StartTournament()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (_areaState.PvpState.HasValue && _areaState.PvpState.Value.InTournament)
        {
            Logging.LogDebug("Already in tournament.");
            return;
        }

        PlayerUtils.SetPlayerInteractionEnabled(_playerState.LocalMainCharacter!.Value, false);
        PlayerUtils.SetLocalPlayerDamageImmunity(_playerState.LocalMainCharacter!.Value, false);
        if (_areaState.OwnsPvpState)
        {
            _areaState.OwnedPvpStateRef().InTournament = true;
            _areaState.OwnedPvpStateRef().InPvP = true;
        }
    }

    private void EndTournament()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
        {
            Logging.LogError("No room joined.");
            return;
        }

        PlayerUtils.SetPlayerInteractionEnabled(_playerState.LocalMainCharacter!.Value, true);
        PlayerUtils.SetLocalPlayerDamageImmunity(_playerState.LocalMainCharacter!.Value, true);
        if (_areaState.OwnsPvpState)
        {
            _areaState.OwnedPvpStateRef().InPvP = false;
            _areaState.OwnedPvpStateRef().InTournament = false;
        }
    }

    [Obsolete("This does not work since on Area join this.AllPlayers are not populated")]
    private int GetSmallerTeamId()
    {
        Dictionary<int, int> teamsCount = [];
        teamsCount[PvpConstants.RedTeamId] = 0;
        teamsCount[PvpConstants.BlueTeamId] = 0;
        teamsCount[PvpConstants.SpectatorTeamId] = 0; // to avoid KeyNotFoundException

        foreach (var (playerId, playerEntity, _) in AllPlayers)
        {
            if (playerId == _state.LocalPlayerId)
                continue;
            var assignedTeamId = playerEntity.GetState().TeamId;
            Logging.LogDebug("Player {PlayerId} in team {TeamId}", playerId, assignedTeamId);
            teamsCount[assignedTeamId]++;
        }

        return teamsCount[PvpConstants.RedTeamId] > teamsCount[PvpConstants.BlueTeamId] ? PvpConstants.RedTeamId : PvpConstants.BlueTeamId;
    }

    private void SetUpRoom()
    {
        var areaEntity = _areaState.CurrentArea;
        if (areaEntity == null)
            return;

        if (_areaState.OwnsPvpState)
        {
            _areaState.OwnedPvpStateRef().InPvP = false;
            _areaState.OwnedPvpStateRef().InTournament = false;
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
        var playerEntity = _playerState.LocalMainCharacter;
        ref var player = ref playerEntity!.Value.GetPvP();

        ClearLoobyCountdown();
        _pvpWidgetManager.SetMainMessage(Texts.InMultiplayer);
        _pvpWidgetManager.SwitchReadyState(player.IsReadyForPvP);
    }

    public void ClearLoobyCountdown()
    {
        _countdownTimer.Reset();
        _pvpWidgetManager.HideCountdown();
    }

    private void RefreshReadyCounts()
    {
        var readyForPvp = OtherPlayers.Count(x => x.Character.GetPvP().IsReadyForPvP && !x.Character.GetPvP().IsSpectator);
        var available = OtherPlayers.Count(x => !x.Character.GetPvP().IsObserver);
        _pvpWidgetManager.UpdateReadyCount(readyForPvp, available);
    }

    private void DestroyTamersOnArena()
    {
        var world = GameUtils.GetWorld();
        var currentLevelId = BGUFuncLibMap.GetCurLevelId(world);
        var levelTamers = LevelTamersConfig.GetLevelTamers(currentLevelId);
        var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(world);
        foreach (var actor in allActorsOfClass)
        {
            var guid = actor.GetFinalGuid();
            if (!levelTamers.Contains(guid))
                actor.CurrentRef.DestroyTamer();
        }
    }

    #region Event Handlers

    private void OnBeginPlayGameplayLevel()
    {
        DestroyTamersOnArena();
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        Logging.LogInformation("Joined room");

        SetUpRoom();
        RefreshReadyCounts();

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
        RefreshReadyCounts();
    }

    private void OnUnitDead(Entity victim, Entity attacker)
    {
        if (_areaState is { PvpState.InPvP: true })
        {
            if (TamerEntity.TryGetTamer(victim, out var victimTamerEntity))
            {
                // FIXME(api): This whole block should be wrapped into a "Replace" utility method so that
                // the policyDir checks can be done jointly.
                if (!_policyDir.TamerEvent<BroadcastUnitSpawnEvent>().CanGameEventNotifyEcs(victimTamerEntity))
                    return;

                var tamerClass = victimTamerEntity.Value.Tamer?.GetClass();
                var netId = victimTamerEntity.Value.GetMeta().NetId;
                var character = victimTamerEntity.Value.Pawn;
                if (character != null && tamerClass != null && tamerClass.PathName == UnitPathUtils.GetUnitPathName(TamerConstants.DaSheng))
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
                                SpawningUtils.SpawnUnitAsOwner(_playerState, _pawnState, _policyDir, TamerConstants.DaSheng2, location, teamId);
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

    private static void ResetPlayer(MainCharacterEntity mainCharacter)
    {
        var pawn = mainCharacter.Pawn!;
        BPS_EventCollectionCS.Get(pawn.PlayerState)?.Evt_TriggerPlayerTransEnd.Invoke(EPlayerTransEndType.None, default);
        var events = BUS_EventCollectionCS.Get(pawn);
        events?.Evt_DestroyAllCtrableBullet.Invoke();
        events?.Evt_TriggerTeleportResetPlayer!.Invoke();
    }

    #endregion

    #region RPC

    public void SendPvPEvent(PvpEvent ev)
    {
        if (!_areaState.OwnsPvpState)
        {
            Logging.LogError("Only room owner can send start countdown.");
            return;
        }

        Logging.LogInformation("Sending PvP event: {Event}", ev);

        _clientRpc.SendPvpEvent([(int)ev.Kind, ev.Data]);
    }

    internal void OnPvpEvent(PlayerId playerId, int[] data)
    {
        var kind = (PvpEventKind)data[0];
        var winnerTeamId = data[1];

        _mappedEvent.InvokeInGameIfApplicable(new PvpEvent(
            kind: kind,
            data: winnerTeamId
        ), default(EmptyContext));
    }

    #endregion
}