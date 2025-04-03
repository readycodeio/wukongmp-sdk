using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using b1.ECS;
using B1UI.GSUI;
using BtlB1;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using Photon.Realtime;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.API;
using WukongApi.Patches;
using WukongApi.State;
using WukongApi.UI;
using PlayerState = WukongApi.State.PlayerState;

namespace WukongApi
{
    // ReSharper disable once InconsistentNaming
    public class WukongMP
    {
        public FreeCameraManager FreeCameraManager { get; } = new();

        private readonly Harmony _harmony = new("WukongMP");

        public WukongClient Photon { get; }

        private bool _isAfterLoadingScreen;

        private readonly TimerWidget _timerWidget = new();
        private readonly LobbyStatusWidget _lobbyStatusWidget = new();
        private readonly GameMessageWidget _gameMessageWidget = new();
        private readonly InfoMessageWidget _infoMessageWidget = new();
        private readonly CountdownWidget _countdownWidget = new();

        public static WukongMP Instance { get; } = new();

        public bool DisableArchiveSave { get; set; }
        public bool IsInitialized { get; private set; }

        private WukongMP()
        {
            Photon = new WukongClient(OnJoinedRoomCallback, p => { GameLoopPatch.QueueOnGameThread(() => AddPlayer(p), "AddPlayer"); });
        }

        public void Patch()
        {
            Utils.TryRunOnGameThread(() =>
            {
                _harmony.PatchCategory(Constants.GlobalPatches);
                _harmony.PatchCategory(Constants.ConnectedPatches);
                Logging.LogInformation("Patched with Harmony");
            });
        }

        public void Unpatch()
        {
            Utils.TryRunOnGameThread(() =>
            {
                _harmony.UnpatchCategory(Constants.ConnectedPatches);
                _harmony.UnpatchCategory(Constants.GlobalPatches);
                Logging.LogInformation("Unpatched with Harmony");
            });
        }

        public void Init()
        {
            // prevent double initialization bug in CSharpLoader
            if (IsInitialized)
                return;

            IsInitialized = true;

            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
                return;

            ConfigurePhotonCallbacks();
            InitGameInstanceAsync();
        }

        public void DeInit()
        {
            IsInitialized = false;
        }

        private void InitGameInstanceAsync()
        {
            Logging.LogInformation("Waiting for the game instance to be initialized.");
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (GameUtils.IsGameInstanceValid())
                        {
                            Logging.LogInformation("Found valid GameInstance");
                            Utils.TryRunOnGameThread(InitWorldCallbacks);
                            break; // Exit the task
                        }

                        await Task.Delay(500);
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                }
            });
        }

        private void InitWorldCallbacks()
        {
            var gameInstance = BGWGameInstanceCS.Get(null);
            if (gameInstance != null)
            {
                BGW_EventCollection.Get(gameInstance).Evt_PostLoadMapWithWorld += OnMapLoaded;
                BGW_EventCollection.Get(gameInstance).Evt_PlayerDelayBeginPlayFinished += OnDelayBeginPlay;
                BGW_EventCollection.Get(gameInstance).Evt_PostPlayerControllerEndPlay += OnEndPlay;
                BGW_EventCollection.Get(gameInstance).Evt_PostLoadingScreenClose += OnLoadingScreenClose;
            }
            else
            {
                Logging.LogError("GameInstance is not valid.");
            }
        }

        private static void OnMapLoaded()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                Logging.LogInformation("New level loaded: {LevelName}", world.GetCurrentLevelName());
            }
        }

        private void OnDelayBeginPlay()
        {
            Logging.LogInformation("Delay begin play for player.");

            // this is triggered for every player controller, but we want to apply the logic once
            if (!Photon.ConnectedAndReady)
            {
                DestroyAllMonsters();
                BlueprintUiUtils.SpawnUiManagerActor();
                InitializeWidgets();
                Photon.StartClient();
            }
        }

        private void OnEndPlay()
        {
            Logging.LogInformation("End play for player.");
            DeinitializeWidgets();
            Photon.StopClient();
        }

        private void InitializeWidgets()
        {
            ChatWidget.Instance.Initialize();
            ChatWidget.Instance.SetVisibility(false);
            _timerWidget.Initialize();
            _lobbyStatusWidget.Initialize();
            _lobbyStatusWidget.SetMaxConnectedCount(Constants.MaxPlayers);
            _gameMessageWidget.Initialize();
            _countdownWidget.Initialize();
            _infoMessageWidget.Initialize();
        }

        private void DeinitializeWidgets()
        {
            ChatWidget.Instance.Deinitialize();
            _timerWidget.Deinitialize();
            _lobbyStatusWidget.Deinitialize();
            _gameMessageWidget.Deinitialize();
            _countdownWidget.Deinitialize();
            _infoMessageWidget.Deinitialize();
        }

        private void OnLoadingScreenClose()
        {
            ChatWidget.Instance.SetVisibility(true);
            if (Photon is { PhotonClient.InRoom: true })
            {
                _isAfterLoadingScreen = true;
                if (Photon.CurrentRoomState.InMatchmaking)
                {
                    var timeDifference = new DateTime(Photon.CurrentRoomState.MatchmakingEndTime, DateTimeKind.Utc) - DateTime.UtcNow;
                    _timerWidget.StartCountdown(0, timeDifference.Seconds, EndMatchmaking);
                    SetupMatchmakingUi();
                }
                else if (Photon.LocalPlayerState.IsSpectator)
                {
                    Logging.LogDebug("Disabling visiblity");
                    SetHudVisibility(false);
                    HideSpectator(Photon.LocalPlayerState);
                    Logging.LogInformation("Entering free camera");
                    FreeCameraManager.EnterFreeCameraMode();
                    TeleportOutSpectator(Photon.LocalPlayerState);
                    SetupSpectatorUi();
                }
                else
                {
                    SetupLobbyUi();
                }
            }
        }

        private void SetupLobbyUi()
        {
            _gameMessageWidget.SetVisibility(true);
            _gameMessageWidget.SetMainText(Texts.InMultiplayer);
            _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(Photon));
            _gameMessageWidget.SetThirdText(Texts.PressToSwitchTeam);
            _lobbyStatusWidget.SetVisibility(true);
        }

        private void SetupMatchmakingUi()
        {
            _gameMessageWidget.SetVisibility(true);
            _gameMessageWidget.SetMainText(Texts.InMultiplayer);
            _gameMessageWidget.SetSecondText(Texts.MatchmakingInProgress);
            _gameMessageWidget.SetThirdText("");
            _lobbyStatusWidget.SetVisibility(true);
        }

        private void SetupSpectatorUi()
        {
            _gameMessageWidget.SetVisibility(true);
            _gameMessageWidget.SetMainText(Texts.InMultiplayer);
            _gameMessageWidget.SetSecondText(Texts.WaitForEnd);
            _gameMessageWidget.SetThirdText("");
            _lobbyStatusWidget.SetVisibility(true);
        }

        public void DumpPlayerState()
        {
            // dump player state to console for me
            Logging.LogDebug("Local player state: {State}", Photon.LocalPlayerState.ToString());
            // dump player state to console for each connected player
            foreach (var (id, state) in Photon.ConnectedPlayers)
            {
                Logging.LogDebug("Player {PlayerId} state: {State}", id, state.ToString());
            }

            // dump synced monsters
            foreach (var (guid, state) in Photon.SyncedMonsters)
            {
                Logging.LogDebug("Monster {Guid} state: {State}", guid, state.ToString());
            }

            // print team hostility info
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            foreach (var (teamId, relation) in teamRelationData.TeamHostileInfos)
            {
                Logging.LogDebug("Team {TeamId} hostility: {HostileTeams}", teamId, string.Join(", ", relation.HostileTeamIDs));
            }
        }

        // annotate that Photon is not null when this returns true

        public bool ShouldRunConnectedPatches()
        {
            return Photon is { ConnectedAndReady: true, PhotonClient.InRoom: true, JoinedRoomCallbacksDone: true };
        }

        public void StartRound()
        {
            _timerWidget.StopCountdown();
            _gameMessageWidget.SetVisibility(false);
            _countdownWidget.StopCountdown();
            _timerWidget.StartCountdown(Constants.RoundMinutes, Constants.RoundSeconds, OnRoundEnded);
            if (Photon.IsMasterClient)
            {
                Photon.CurrentRoomState.InCombatRound = true;
                if (Photon.CurrentRoomState.BotsEnabled && Photon.ConnectedPlayers.Count == 0)
                {
                    GameLoopPatch.QueueOnGameThread(() => SpawnBots(), "SpawnBots");
                }
            }
        }

        private void OnRoundEnded()
        {
            Logging.LogInformation("Round time ended, ending round");
            if (Photon.IsMasterClient)
            {
                Task.Run(async () => await Photon.LobbyManager.EndRoundAsync(Constants.DrawTeamId));
            }
        }

        public void EndRound()
        {
            _timerWidget.StopCountdown();

            if (Photon.IsMasterClient)
            {
                Photon.CurrentRoomState.InCombatRound = false;
                foreach (var playerState in Photon.AllConnectedPlayers)
                {
                    var events = BUS_EventCollectionCS.Get(playerState.Pawn);
                    events?.Evt_RelieveImmobilized.Invoke();
                    events?.Evt_RelievePhantomRush.Invoke();
                }
            }
        }

        public void EnablePvP()
        {
            Logging.LogInformation("Enabled PvP");

            var myTeam = Photon.LocalPlayerState.TeamId;
            var otherTeams = Photon.ConnectedPlayers.Values
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
                    PhotonUtils.RegisterTeamHostility(myTeam, team);
                }
            }, "Register team hostility");
        }

        public void DisablePvP()
        {
            Logging.LogInformation("Disabled PvP");

            var myTeam = Photon.LocalPlayerState.TeamId;
            var otherTeams = Photon.ConnectedPlayers.Values
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
                    PhotonUtils.UnregisterTeamHostility(myTeam, team);
                }
            }, "Register team hostility");
        }

        public void EndTournament(int winnerTeamId)
        {
            Logging.LogInformation("End tournament");
            SetupLobbyUi();
            ShowSpectatingPlayers();
            FreeCameraManager.LeaveFreeCameraMode();
            SetHudVisibility(true);
        }

        private void ShowSpectatingPlayers()
        {
            foreach (var playerState in Photon.SpectatingPlayers)
            {
                ShowSpectator(playerState);
                _lobbyStatusWidget.UpdatePlayerTeam(playerState, playerState.TeamId);
            }
        }

        public void TeleportSpectatingPlayers()
        {
            foreach (var playerState in Photon.SpectatingPlayers)
            {
                TeleportInSpectator(playerState);
            }
        }

        private void WakeUpMonster(string guid)
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                if (BGU_DataUtil.GetActorGuid(actor) != guid)
                    continue;

                var events = BGS_GSEventCollection.Get(actor);
                if (events != null)
                {
                    if (actor.GetMonster() == null)
                    {
                        Logging.LogDebug("Spawning monster for tamer with guid: {Guid}.", guid);

                        if (!Photon.SyncedMonsters.ContainsKey(guid))
                        {
                            Photon.SyncedMonsters.Add(guid, new MonsterState(guid, actor, actor.GetMonsterClass().PathName));
                            Logging.LogDebug("Monster was not synced, adding to synced monsters.");
                        }

                        Logging.LogDebug("Invoking Evt_TamerBlockingSpawnImmediately.");
                        events.Evt_TamerBlockingSpawnImmediately.Invoke(guid);
                    }
                    else if (!Photon.SyncedMonsters.ContainsKey(guid))
                    {
                        Logging.LogDebug("Monster already spawned but not synced: {Guid}.", guid);

                        var state = new MonsterState(guid, actor, actor.GetMonsterClass().PathName);
                        Photon.SyncedMonsters.Add(guid, state);

                        PhotonUtils.PrepareMonsterForSync(Photon, state);
                    }
                }
                else
                {
                    Logging.LogDebug("Event is null");
                }

                return;
            }

            // TODO: Spawn if not found
        }

        private void RebirthPlayer(int playerId)
        {
            Logging.LogDebug("RebirthPlayer for player {PlayerId} called", playerId);

            var player = Photon.GetById(playerId);
            if (player == null)
                return;

            if (player.PhotonId == Photon.LocalPlayerState.PhotonId)
            {
                FreeCameraManager.LeaveFreeCameraMode();
            }

            var events = BUS_EventCollectionCS.Get(player.Pawn);
            if (events != null)
            {
                events.Evt_OnLeaveFalling.Invoke(); // Reset falling timer.
                events.Evt_RebirthTeleportFinish.Invoke(ERebirthType.RebirthPoint); // Rest state and play anim montage.
                events.Evt_TriggerTeleportResetPlayer.Invoke(); // Reset player stats, will set IsDead flag to false.
            }
        }

        private static void DestroyAllMonsters()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                BGU_UnrealWorldUtil.DestroyActor(actor);
            }
        }

        private void ConfigurePhotonCallbacks()
        {
            if (Photon.ConnectedAndReady)
            {
                Logging.LogError("Photon is already connected and ready");
                return;
            }

            Photon.OnBeforeJoinRoom += SetPlayerProperties;
            Photon.OnUnitSpawn += (_, guid, name, teamId, x, y, z) => GameLoopPatch.QueueOnGameThread(() => SpawnRemoteUnit(guid, name, teamId, x, y, z), "SpawnRemoteUnit");
            Photon.OnMontageCallback += (id, data) => GameLoopPatch.QueueOnGameThread(() => ApplyPlayerMontageCallback(id, data), "ApplyPlayerMontageCallback");
            Photon.OnMonsterMontageCallback += (id, data) => GameLoopPatch.QueueOnGameThread(() => ApplyMonsterMontageCallback(id, data), "ApplyMonsterMontageCallback");
            Photon.OnMonsterWakeUp += guid => GameLoopPatch.QueueOnGameThread(() => WakeUpMonster(guid), "WakeUpMonster");
            Photon.OnEquipmentChange += (id, eq) => GameLoopPatch.QueueOnGameThread(() => ChangeEquipment(id, eq), "ChangeEquipment");
            Photon.OnReadinessChange += (name, isReady, readyCount) => Utils.TryRunOnGameThread(() => UpdateReadiness(name, isReady, readyCount));
            Photon.OnTeamChange += (playerState, teamId) => Utils.TryRunOnGameThread(() => UpdatePlayerTeam(playerState, teamId));
            Photon.OnPlayerLeft += playerState => Utils.TryRunOnGameThread(() => RemovePlayer(playerState));
            Photon.OnDamageNum += damageNum => GameLoopPatch.QueueOnGameThread(() => OnDamageNum(damageNum), "OnDamageNum", BGW_TickGroupMask.TG_PreAnim);
            Photon.OnPlayerRebirth += id => GameLoopPatch.QueueOnGameThread(() => RebirthPlayer(id), "RebirthPlayer");
            Photon.OnKillPlayer += id => GameLoopPatch.QueueOnGameThread(() => KillPlayer(id), "KillPlayer");
            Photon.OnSetPlayerTransform += (loc, rot) => GameLoopPatch.QueueOnGameThread(() => SetLocalPlayerTransform(loc, rot), "SetPlayerTransform");
            Photon.OnPhantomRush += (id, direction) => GameLoopPatch.QueueOnGameThread(() => PerformPhantomRush(id, direction), "PerformPhantomRush");
            Photon.OnExitPhantomRush += (id) => GameLoopPatch.QueueOnGameThread(() => ExitPhantomRush(id), "ExitPhantomRush");
            Photon.OnHandleImmobilize += (id, otherId, type, hasBuff) => GameLoopPatch.QueueOnGameThread(() => HandleImmobilize(id, otherId, type, hasBuff), "HandleImmobilize");
            Photon.OnTargetSet += (playerId, targetId) => GameLoopPatch.QueueOnGameThread(() => OnTargetSet(playerId, targetId), "OnTargetSet");
            Photon.OnMatchmakingEnded += () => GameLoopPatch.QueueOnGameThread(OnMatchmakingEnded, "OnMatchmakingEnded");
            Photon.OnBuffAdded += (playerId, buffId, duration) => GameLoopPatch.QueueOnGameThread(() => OnBuffAdded(playerId, buffId, duration), "OnBuffAdded");
            Photon.OnBuffRemoved += (playerId, a, b, c, d) => GameLoopPatch.QueueOnGameThread(() => OnBuffRemoved(playerId, a, b, c, d), "OnBuffRemoved");
            Photon.OnBuffAllRemoved += (playerId, a, b) => GameLoopPatch.QueueOnGameThread(() => OnBuffAllRemoved(playerId, a, b), "OnBuffAllRemoved");
        }

        private void OnBuffAdded(int playerId, int buffId, float duration)
        {
            var playerState = Photon.GetById(playerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {Id}", playerId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for player {Nickname}", playerState.NickName);
                return;
            }

            Logging.LogDebug("Adding buff {BuffId} to player {Nickname} with duration {Duration}", buffId, playerState.NickName, duration);
            events.Evt_BuffAdd.Invoke(buffId, playerState.Pawn, playerState.Pawn, duration);
        }

        private void OnBuffRemoved(int playerId, int buffId,
            EBuffEffectTriggerType removeTriggerType,
            int inLayer,
            bool withTriggerRemoveEffect)
        {
            var playerState = Photon.GetById(playerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {Id}", playerId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for player {Nickname}", playerState.NickName);
                return;
            }

            Logging.LogDebug("Removing buff {BuffId} from player {Nickname}, type: {Type}", buffId, playerState.NickName, removeTriggerType);
            events.Evt_BuffRemove.Invoke(buffId, removeTriggerType, inLayer, withTriggerRemoveEffect);
        }

        private void OnBuffAllRemoved(int playerId, EBuffEffectTriggerType removeTriggerType, bool withTriggerRemoveEffect)
        {
            var playerState = Photon.GetById(playerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {Id}", playerId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for player {Nickname}", playerState.NickName);
                return;
            }

            Logging.LogDebug("Removing all buffs from player {Nickname}, type: {Type}", playerState.NickName, removeTriggerType);
            events.Evt_BuffAllRemove.Invoke(removeTriggerType, withTriggerRemoveEffect);
        }

        private void ExitPhantomRush(int playerId)
        {
            var playerState = Photon.GetById(playerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {Id}", playerId);
                return;
            }

            Logging.LogDebug("Received exit phantom rush for player {Nickname}", playerState.NickName);
            var events = BUS_EventCollectionCS.Get(playerState.Pawn);
            playerState.ReceivedPhantomRushExit = true;
            events?.Evt_RelievePhantomRush.Invoke();
        }

        private void OnTargetSet(int playerId, int targetId)
        {
            if (!Photon.ConnectedPlayers.TryGetValue(playerId, out var playerState))
            {
                Logging.LogError("Player not found: {Id}", playerId);
                return;
            }

            var targetPlayerState = Photon.GetById(targetId);
            if (targetPlayerState == null)
            {
                Logging.LogError("Player not found: {Id}", targetId);
                return;
            }

            Logging.LogDebug("Updating player target for player {PlayerNickname} to player {TargetNickname}", playerState.NickName, targetPlayerState.NickName);

            var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(playerState.Pawn);
            targetInfoData.SetTargetInfo(new UnitLockTargetInfo(targetPlayerState.Pawn, ETargetSourceType.SkillBase_NormalUse));
        }

        private void UpdatePlayerTeam(PlayerState playerState, int teamId)
        {
            Logging.LogDebug("Updating player {Nickname} to team {Team}", playerState.NickName, teamId);

            var player = playerState.Pawn as BGUCharacterCS;

            if (player == null)
            {
                Logging.LogError("Failed to cast pawn to BGUCharacterCS");
                return;
            }

            PhotonUtils.RegisterNewPlayerTeam(player, teamId);

            if (playerState.MarkerActor != null)
            {
                var teamName = GameUtils.GetTeamName(playerState.TeamId);
                playerState.MarkerActor.CallFunctionByNameWithArguments($"SetText {playerState.NickName} {teamName}", true);
            }

            UpdatePlayerTeamUi(playerState);
        }

        private void UpdatePlayerTeamUi(PlayerState playerState)
        {
            if (!playerState.IsSpectator)
                _lobbyStatusWidget.UpdatePlayerTeam(playerState, playerState.TeamId);
        }

        private void KillPlayer(int playerId)
        {
            var player = Photon.GetById(playerId)?.Pawn;
            if (player == null)
                return;

            var events = BUS_EventCollectionCS.Get(player);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            if (Photon.IsMasterClient)
            {
                events?.Evt_UnitDead.Invoke(player, EDeadReason.Suicide);
            }
        }

        private void SetLocalPlayerTransform(FVector location, FRotator rotation)
        {
            GameUtils.GetBguPlayerCharacterCs()?.SetActorTransform(new FTransform(rotation, location), false, out _, true);
            GameUtils.GetPlayerController()?.SetControlRotation(rotation);
        }

        private void PerformPhantomRush(int playerId, ESkillDirection direction)
        {
            var playerState = Photon.GetById(playerId);
            if (playerState?.Pawn == null)
            {
                Logging.LogError("Player not found: {PlayerId}", playerId);
                return;
            }

            Logging.LogDebug("Received phantom rush for player {Nickname} in direction {Direction}", playerState.NickName, direction);
            var events = BUS_EventCollectionCS.Get(playerState.Pawn);
            events?.Evt_TriggerPhantomRush.Invoke(direction);

            ResetCooldown(playerState.Pawn);
            ResetMana(playerState.Pawn);
        }

        public static void ResetLocalPlayerCooldown()
        {
            var player = GameUtils.GetBguPlayerCharacterCs();

            if (player == null)
            {
                Logging.LogError("Failed to get player");
                return;
            }

            ResetCooldown(player);
            ResetMana(player);
        }

        private static void SetHudVisibility(bool visible)
        {
            GenABattleMain.SetBattleMainTempHide(!visible, "TickUpdateUIShowState");
        }

        private static void ResetCooldown(APawn playerPawn)
        {
            var events = BUS_EventCollectionCS.Get(playerPawn);
            events?.Evt_ResetSkillCD.Invoke();
        }

        private static void ResetMana(APawn playerPawn)
        {
            var events = BUS_EventCollectionCS.Get(playerPawn);
            var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(playerPawn);
            float maxMana = attrContainer.GetFloatValue(EBGUAttrFloat.MpMax);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.Mp, maxMana);
        }

        private void HandleImmobilize(int playerId, int otherPlayerId, ImmobilizeActionType immobilizeAction, bool hasBuff)
        {
            var playerState = Photon.GetById(playerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {Id}", playerId);
                return;
            }

            var otherPlayerState = Photon.GetById(otherPlayerId);

            switch (immobilizeAction)
            {
                case ImmobilizeActionType.Cast:
                    CastImmobilize(playerState);
                    break;
                case ImmobilizeActionType.Trigger:
                    if (otherPlayerState == null)
                    {
                        Logging.LogError("Player not found: {Id}", otherPlayerId);
                        return;
                    }

                    TriggerImmobilize(playerState, otherPlayerState, hasBuff);
                    break;
                case ImmobilizeActionType.Relieve:
                    RelieveImmobilize(playerState);
                    break;
                case ImmobilizeActionType.Break:
                // Currently not supported
                default:
                    Logging.LogError("Unknown ImmobilizeActionType: {Action}", immobilizeAction);
                    break;
            }
        }

        private void CastImmobilize(PlayerState castingPlayerState)
        {
            if (Photon.IsMasterClient)
            {
                Logging.LogDebug("Received cast immobilize for player {Nickname}", castingPlayerState.NickName);
                var playerEvents = BUS_EventCollectionCS.Get(castingPlayerState.Pawn);
                playerEvents.Evt_CastImmobilize.Invoke(0);
            }
        }

        private static void TriggerImmobilize(PlayerState immobilizedPlayerState, PlayerState castingPlayerState, bool hasBuff)
        {
            Logging.LogDebug("Received trigger immobilize for player {Nickname}", immobilizedPlayerState.NickName);

            if (immobilizedPlayerState.Pawn is not BGUCharacterCS character)
            {
                Logging.LogError("Failed to cast pawn to BGUCharacterCS");
                return;
            }

            var castImmobilizeData = (BUC_CastImmobilizeData)character.GetDataByChunk(TypeManager.GetTypeIndex<BUC_CastImmobilizeData>());

            var cachedImmobilizeConfigDesc = castImmobilizeData.GetCachedImmobilizeConfigDesc(castImmobilizeData.ResId);
            if (cachedImmobilizeConfigDesc == null)
            {
                return;
            }

            if (castingPlayerState.Pawn == null)
            {
                Logging.LogError("Casting player pawn is null");
                return;
            }

            var immobilizeConfigInstance = GameUtils.CreateImmobilizeConfig(character, castingPlayerState.Pawn, cachedImmobilizeConfigDesc, castImmobilizeData.ResId, hasBuff);
            BUS_EventCollectionCS.Get(character)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);
        }

        private static void RelieveImmobilize(PlayerState immobilizedPlayerState)
        {
            Logging.LogDebug("Received relieve immobilize for player {Nickname}", immobilizedPlayerState.NickName);
            var playerEvents = BUS_EventCollectionCS.Get(immobilizedPlayerState.Pawn);
            immobilizedPlayerState.RunImmobilizePatches = true;
            playerEvents?.Evt_RelieveImmobilized.Invoke();
        }

        private void SetPlayerProperties()
        {
            var player = GameUtils.GetControlledPawn();

            if (player == null)
            {
                Logging.LogError("Failed to get controlled pawn");
                return;
            }

            Logging.LogDebug("Setting initial player properties");

            Photon.CachePlayerProperty(nameof(PlayerState.Location), player.GetActorLocation());
            Photon.CachePlayerProperty(nameof(PlayerState.Rotation), player.GetActorRotation());

            // equipment
            var eq = EquipmentHelpers.GetCurrentEquipmentStateForActor(player);
            Photon.CachePlayerProperty(nameof(PlayerState.Equipment), eq);

            // attributes
            var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(player);
            foreach (var attr in Constants.SyncedAttributes)
            {
                var value = attrs.GetFloatValue(attr);
                Photon.CachePlayerAttribute(attr, value);
            }

            // hp
            var hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
            Photon.CachePlayerProperty(nameof(PlayerState.Hp), hp);

            Photon.SetCachedPlayerProperties();
            Logging.LogDebug("Finished setting initial player properties");
        }

        private void ChangeEquipment(int id, EquipmentState eq)
        {
            if (id == Photon.LocalPlayerState.PhotonId)
                return;

            if (!Photon.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Logging.LogError("Player not found: {PlayerId}", id);
                return;
            }

            if (player.Pawn is not BGUCharacterCS pawn)
            {
                Logging.LogWarning("Failed to cast pawn to BGUCharacterCS");
                return;
            }

            EquipmentHelpers.SetRemoteActorEquipment(pawn, eq);
        }

        private void UpdateReadiness(string playerNickName, bool isReady, int readyCount)
        {
            if (Photon.IsMasterClient) // send this only once
            {
                Photon.WukongChat.SendServerMessage($"{playerNickName} is {(isReady ? "ready" : "not ready")}");
            }

            if (isReady)
            {
                if ((Photon.ConnectedPlayers.Count > 0 || Photon.CurrentRoomState.BotsEnabled) && readyCount == Photon.ConnectedPlayers.Count + 1)
                {
                    // all players are ready
                    _gameMessageWidget.SetMainText(Texts.StartingGame);
                    _countdownWidget.StartLobbyCountdown(Constants.CountdownSeconds, Photon.StartPvP);
                }

                _lobbyStatusWidget.SetReadyCount(readyCount);
            }
            else
            {
                _countdownWidget.StopCountdown();
                _gameMessageWidget.SetMainText(Texts.InMultiplayer);
                _lobbyStatusWidget.SetReadyCount(readyCount);
            }
        }

        public void SwitchReadyState(bool isReady)
        {
            if (isReady)
            {
                _gameMessageWidget.SetThirdText(Texts.YouAreReady);
                _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(Photon));
            }
            else
            {
                _gameMessageWidget.SetThirdText(Texts.PressToSwitchTeam);
                _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(Photon));
            }
        }

        public void RemovePlayer(PlayerState playerState)
        {
            if (playerState.MarkerActor != null)
            {
                BGU_UnrealWorldUtil.DestroyActor(playerState.MarkerActor);
            }

            if (playerState.Pawn != null)
            {
                BGU_UnrealWorldUtil.DestroyActor(playerState.Pawn);
            }

            _lobbyStatusWidget.RemovePlayerFromTeams(playerState);
            UpdateConnectedCount();
            _lobbyStatusWidget.SetReadyCount(Photon.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
        }

        private void UpdateConnectedCount()
        {
            _lobbyStatusWidget.SetConnectedCount(Photon.ConnectedPlayers.Count + 1);
            if (!Photon.LocalPlayerState.IsReadyForPvP)
            {
                _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(Photon));
            }
        }

        private static void OnDamageNum(DamageNumParam damageNum)
        {
            var uiEvt = BGW_UIEventCollection.Get(GameUtils.GetWorld());
            uiEvt.Evt_UI_ShowHPChangeNum(damageNum);
        }

        public void ApplyPlayerMontageCallback(int id, MontageCallbackData data)
        {
            var player = Photon.AllConnectedPlayers.FirstOrDefault(x => x.PhotonId == id);
            if (player == null)
            {
                Logging.LogError("Player not found: {PlayerId}", id);
                return;
            }

            var clone = player.Pawn as ACharacter;

            if (clone == null)
            {
                Logging.LogError("Failed to cast pawn to ACharacter");
                return;
            }

            if (string.IsNullOrEmpty(data.ShortMontagePath))
            {
                Logging.LogDebug("Stopping montage playback for player {PlayerId}", id);
                clone.StopAnimMontage(null);
                return;
            }

            var fullMontagePath = MontageHelpers.DecompressMontageName(data.ShortMontagePath);
            Logging.LogDebug("Received montage: {Montage}, position: {Position}, reset: {Reset}", fullMontagePath, data.Position, data.Reset);

            var animInstance = clone.Mesh.GetAnimInstance();
            if (animInstance == null)
            {
                Logging.LogError("AnimInstance is null");
                return;
            }

            var currentMontage = animInstance.GetCurrentActiveMontage();
            Logging.LogDebug("Current montage: {Montage}", currentMontage?.PathName);

            // if the same montage is currently playing an no reset flag is given, do not play new montage
            if (currentMontage != null && currentMontage.PathName == fullMontagePath && !data.Reset)
            {
                Logging.LogDebug("Skipping montage playback: {Montage}, is reset: {Reset}", fullMontagePath, data.Reset);
                return;
            }

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage == null)
            {
                Logging.LogWarning("Montage not found: {Montage}", fullMontagePath);
                return;
            }

            var events = BUS_EventCollectionCS.Get(clone);

            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            Logging.LogDebug("Applying montage callback for player {PlayerId} with montage {Montage} @ {Position}", id, fullMontagePath, data.Position);
            animInstance.Montage_Play(montage, 1f, EMontagePlayReturnType.MontageLength, data.Position);
            events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
        }

        private void ApplyMonsterMontageCallback(int _, MonsterMontageCallbackData data)
        {
            if (!Photon.SyncedMonsters.TryGetValue(data.MonsterGuid, out var monster))
            {
                Logging.LogWarning("Monster not found: {Guid}", data.MonsterGuid);
                return;
            }

            if (!monster.IsTamerValid)
                return;

            var tamerActor = monster.Pawn;

            if (tamerActor == null)
            {
                Logging.LogError("Tamer actor is null");
                return;
            }

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(data.MontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage == null)
            {
                Logging.LogWarning("Montage not found: {Montage}", data.MontagePath);
                return;
            }

            Logging.LogDebug("Applying montage callback for monster {Guid} with montage {Montage} ({Reason}, {State})", data.MonsterGuid, data.MontagePath, data.Reason, data.State);
            if (tamerActor.GetMonster() == null)
            {
                Logging.LogError("Monster is null");
                return;
            }

            var animInstance = tamerActor.GetMonster().Mesh.GetAnimInstance();

            if (data.State == EMontageCallbackState.OnStarted)
            {
                animInstance.Montage_Play(montage);
            }
            else if (data.State == EMontageCallbackState.OnInterrupted)
            {
                if (animInstance.GetCurrentActiveMontage().PathName == montage.PathName)
                {
                    animInstance.Montage_Stop(1f, montage);
                }
            }

            var events = BUS_EventCollectionCS.Get(tamerActor);
            if (events != null)
            {
                events.Evt_PlayMontageCallback.Invoke(data.Reason, montage, data.State);
            }
            else
            {
                Logging.LogError("events are null");
            }
        }

        public void SpawnEnemiesMaster(string enemyName, int count, int teamId)
        {
            var player = GameUtils.GetControlledPawn();

            if (player == null)
            {
                Logging.LogError("Failed to get controlled pawn");
                return;
            }

            var traceLoc = player.GetActorLocation() + player.GetActorForwardVector() * Constants.MonsterSpawnDistance + FVector.UpVector * Constants.MonsterSpawnTraceHeight / 2;

            // trace vertically for spawn height
            var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), traceLoc, traceLoc - FVector.UpVector * Constants.MonsterSpawnTraceHeight, out var hitResultSimple);
            FVector centerLoc;
            if (hit)
            {
                centerLoc = hitResultSimple.HitLocation + FVector.UpVector * Constants.MonsterHalfHeight;
                Logging.LogDebug("Spawning enemy by line trace");
            }
            else
            {
                centerLoc = player.GetActorLocation() + player.GetActorForwardVector() * Constants.MonsterSpawnDistance;
                Logging.LogDebug("Spawning enemy by player forward vector");
            }

            // spawn in a spiral around center point, separated by 100 units
            var dAngle = 2 * FMath.PI / FMath.Min(count, 6);
            for (var i = 0; i < count; i++)
            {
                var angle = i * dAngle;
                var radius = i * Constants.MonsterSpawnSpread;
                var loc = centerLoc + new FVector(FMath.Cos(angle), FMath.Sin(angle), 0) * radius;

                var localI = i;
                Task.Run(async () =>
                {
                    // wait for i * 200ms
                    await Task.Delay(localI * Constants.MonsterSpawnDelayMs);
                    GameLoopPatch.QueueOnGameThread(() => { SpawnEnemyMaster(enemyName, loc, teamId); }, "SpawnEnemyMaster");
                });
            }
        }

        private void SpawnEnemyMaster(string enemyName, FVector loc, int teamId)
        {
            var unitName = UnitPathsConfig.GetUnitPath(enemyName);

            var id = Guid.NewGuid().ToString(); // TODO: use ActorGuid
            SpawnUnitLocally(id, unitName, teamId, loc.X, loc.Y, loc.Z);

            Logging.LogDebug("Sending spawn enemy {Name} at {Location}", enemyName, loc.ToCompactString());
            Photon.SpawnUnit(id, unitName, teamId, loc.X, loc.Y, loc.Z);
        }

        private void SpawnRemoteUnit(string guid, string unitName, int teamId, float x, float y, float z)
        {
            SpawnUnitLocally(guid, unitName, teamId, x, y, z);
        }

        private void SpawnUnitLocally(string guid, string unitName, int teamId, float x, float y, float z)
        {
            Logging.LogDebug("Spawn unit called for {UnitName}", unitName);

            if (string.IsNullOrEmpty(unitName))
                return;

            var loc = new FVector(x, y, z);
            var rot = new FRotator();

            var world = GameUtils.GetWorld();

            var cachedResourceObj = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitName, ELoadResourceType.SyncLoadAndCache);
            var transform = new FTransform(rot, loc);
            var buTamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, (TSubclassOf<AActor>)cachedResourceObj, transform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as BUTamerActor;
            if (buTamerActor == null)
            {
                Logging.LogError("Could not spawn enemy: {UnitName}", unitName);
                return;
            }
            buTamerActor.MarkAsSpawnedTamer(null);
            buTamerActor.ExtendConfigComp.ActorResetType = EBGUResetType.Destroy;

            buTamerActor.SpawnedTamerGuid = guid;
            // Update final guid
            buTamerActor.GetFinalGuid(true);

            UBGUFunctionLibrary.BGUFinishSpawningActor(buTamerActor, transform);
            Logging.LogDebug("Spawned enemy: {TamerName}, with Guid {Guid}", buTamerActor.GetName(), guid);
            var monsterState = new MonsterState(guid, buTamerActor, teamId, unitName);
            Photon.SyncedMonsters.Add(guid, monsterState);
            BGS_GSEventCollection.Get(buTamerActor)?.Evt_TamerBlockingSpawnImmediately.Invoke(guid);

            monsterState.NickName = "Bot";
            CreateMarkerForCharacter(monsterState); // 3D marker above monster
            if (unitName == UnitPathsConfig.GetUnitPath(CharacterKind.Monkey))
            {
                SetMonkeyBotConfig(buTamerActor.GetMonster());
            }
        }

        private void SetMonkeyBotConfig(BGUCharacterCS bGUCharacter)
        {
            var events = BUS_EventCollectionCS.Get(bGUCharacter);
            if (events != null)
            {
                foreach (var attr in MonkeyBotConfig.Attribues)
                {
                    events.Evt_SetAttrFloat.Invoke(attr.Key, attr.Value);
                }
                foreach (var eq in MonkeyBotConfig.Equipment)
                {
                    events.Evt_InitDaShenEquipData.Invoke(eq.Key, eq.Value);
                }
            }
        }

        public void SpawnBots()
        {
            for (int i = 0; i < Constants.BotCount; i++)
            {
                float angle = i / (float)Constants.BotCount * 2f * FMath.PI;
                float x = FMath.Cos(angle) * Constants.PvpMonsterRadius;
                float y = FMath.Sin(angle) * Constants.PvpMonsterRadius;

                FVector spawnPosition = Constants.PvpStartingLocation + new FVector(x, y, 0f);
                SpawnEnemyMaster(CharacterKind.Monkey, spawnPosition, GameUtils.GetOppositeTeam(Photon.LocalPlayerState.TeamId));
            }
        }

        private void OnJoinedRoomCallback()
        {
            TeleportLocalPlayerOnStart(Photon.LocalPlayerState.PhotonId);
            SetupSpectator();
            SpawnPlayersAlreadyInRoom();
            UpdateConnectedCount();
            DisablePlayerSkills();
            _lobbyStatusWidget.SetReadyCount(Photon.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
            _lobbyStatusWidget.SetMaxConnectedCount(Photon.PhotonClient.CurrentRoom.MaxPlayers);
            SetupMatchmaking();
        }

        private void TeleportLocalPlayerOnStart(int playerId)
        {
            var spawnPosition = GetSpawnPosition(playerId);
            SetLocalPlayerTransform(spawnPosition, FRotator.ZeroRotator);
        }

        private FVector GetSpawnPosition(int playerId)
        {
            int maxPlayersCount = Photon.PhotonClient.CurrentRoom.MaxPlayers;

            float angle = playerId / (float)maxPlayersCount * 2f * FMath.PI;
            float x = FMath.Cos(angle) * Constants.PvpStartingRadius;
            float y = FMath.Sin(angle) * Constants.PvpStartingRadius;

            return Constants.PvpStartingLocation + new FVector(x, y, 0f);
        }

        private void SetupSpectator()
        {
            if (Photon.IsMasterClient)
            {
                Photon.CurrentRoomState.InPvP = false;
            }
            else if (Photon.CurrentRoomState.InPvP)
            {
                Logging.LogDebug("Setting IsSpectator to true");
                Photon.CachePlayerProperty(nameof(PlayerState.IsSpectator), true);
                Logging.LogDebug("Setting cached properties");
                Photon.SetCachedPlayerProperties();
            }
        }

        private void SetupMatchmaking()
        {
            if (Photon.CurrentRoomState.GameMode == GameMode.Private)
                return;

            if (Photon.IsMasterClient)
            {
                Photon.CurrentRoomState.InMatchmaking = true;
                Photon.CurrentRoomState.MatchmakingEndTime = DateTime.UtcNow.AddSeconds(Constants.MatchmakingSeconds).Ticks;
            }
        }

        private void EndMatchmaking()
        {
            if (Photon.IsMasterClient)
            {
                Photon.CurrentRoomState.InMatchmaking = false;
                Photon.SendEndMatchmaking();
            }

            _timerWidget.StopCountdown();
        }

        private void OnMatchmakingEnded()
        {
            _timerWidget.StopCountdown();
            if (_isAfterLoadingScreen)
            {
                SetupLobbyUi();
            }
        }

        private static void DisablePlayerSkills()
        {
            var player = GameUtils.GetBguPlayerCharacterCs();
            var events = BUS_EventCollectionCS.Get(player);
            if (events != null)
            {
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInVigorSkill);
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantCastFaBao);
            }
        }

        private void DisablePlayerInteraction()
        {
            var player = GameUtils.GetBguPlayerCharacterCs();
            var events = BUS_EventCollectionCS.Get(player);
            if (events != null)
            {
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInteract);
            }
        }

        private void SpawnPlayersAlreadyInRoom()
        {
            // when joining game, spawn all players already in room
            foreach (var player in Photon.GetOtherPlayersInRoom())
            {
                GameLoopPatch.QueueOnGameThread(() => AddPlayer(player), "AddPlayer");
            }
        }

        public static BGUCharacterCS? SpawnWukong(ABGPPlayerController oldController, UClass pawnClass, FTransform spawnTransform, APawn oldPawn)
        {
            var newPawn = BGU_UnrealActorUtil.BGUBeginDeferredActorSpawnFromClass(oldController.World, pawnClass, spawnTransform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as APawn;
            oldController.Possess(newPawn);

            if (newPawn is not BGUCharacterCS newCharacter)
            {
                Logging.LogError("Failed to cast pawn to ACharacter");
                return null;
            }

            newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: false);
            newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: false);
            BGU_UnrealActorUtil.BGUFinishSpawningActorAndECSBeginPlay(oldController, newCharacter, spawnTransform);
            BPS_GSEventCollection.Get(oldController).Evt_BPS_OnControlledPawnChange.Invoke(newCharacter);
            BGS_EventCollectionCS.Get(oldController)?.Evt_NotifyPossessEntityChanged.Invoke(oldPawn.ToEntity(), newCharacter.ToEntity());
            newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
            newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
            UGSE_ActorFuncLib.UpdateActorOverlaps(newCharacter);
            return newCharacter;
        }

        public static void BackToOldPawn(ABGPPlayerController oldController, APawn oldPawn, APawn newPawn)
        {
            oldController.UnPossess();
            oldController.Possess(oldPawn);
            BPS_GSEventCollection.Get(oldController).Evt_BPS_OnControlledPawnChange.Invoke(oldPawn);
            BGS_EventCollectionCS.Get(oldController)?.Evt_NotifyPossessEntityChanged.Invoke(newPawn.ToEntity(), oldPawn.ToEntity());
        }

        private void AddPlayer(Player player)
        {
            var playerState = SpawnCloneForPlayer(player);

            if (playerState != null)
            {
                CreateMarkerForCharacter(playerState); // 3D marker above player
                Photon.RegisterPlayer(playerState);
                UpdateConnectedCount();

                var readyForPvP = false;
                if (player.CustomProperties.TryGetValue(nameof(PlayerState.IsReadyForPvP), out var isReady))
                {
                    readyForPvP = (bool)isReady;
                }

                if (Photon.CurrentRoomState.InPvP && !readyForPvP)
                {
                    HideSpectator(playerState);
                    TeleportOutSpectator(playerState);
                }
                else
                {
                    UpdatePlayerTeamUi(playerState);
                }

                if (Photon.AllConnectedPlayers.Count() == Photon.PhotonClient.CurrentRoom.MaxPlayers)
                {
                    EndMatchmaking();
                }
            }
        }

        private void HideSpectator(PlayerState playerState)
        {
            SetPlayerVisibility(playerState, false);
        }

        private void TeleportOutSpectator(PlayerState playerState)
        {
            playerState.Pawn?.SetActorTransform(FTransform.Identity, false, out _, true);
        }


        private void ShowSpectator(PlayerState playerState)
        {
            SetPlayerVisibility(playerState, true);
        }

        private void TeleportInSpectator(PlayerState playerState)
        {
            var spawnPosition = GetSpawnPosition(playerState.PhotonId);
            playerState.Pawn?.SetActorTransform(new FTransform(FRotator.ZeroRotator, spawnPosition), false, out _, true);
        }

        public static void SetPlayerVisibility(PlayerState playerState, bool visible)
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

        private static void SetPlayerCollision(PlayerState playerState, bool enabled)
        {
            Logging.LogDebug("Setting player {PlayerName} collision to: {Enabled}", playerState.NickName, enabled);

            if (playerState.Pawn == null)
            {
                Logging.LogError("Player pawn is null");
                return;
            }

            playerState.Pawn.SetActorEnableCollision(enabled);
        }

        private PlayerState? SpawnCloneForPlayer(Player player)
        {
            var id = player.ActorNumber;

            if (Photon.ConnectedPlayers.ContainsKey(id))
            {
                Logging.LogDebug("Player already exists: {Id}", id); // reconnection
                return null;
            }

            var playerPawnClass = GameUtils.GetControlledPawn()?.GetClass();

            if (playerPawnClass == null)
            {
                Logging.LogError("Player pawn class is null");
                return null;
            }

            var oldPawn = GameUtils.GetControlledPawn();

            if (oldPawn == null)
            {
                Logging.LogError("Old pawn is null");
                return null;
            }

            FVector loc = default;
            FRotator rot = default;

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.Location), out var playerLoc))
            {
                loc = (FVector)playerLoc;
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.Rotation), out var playerRot))
            {
                rot = (FRotator)playerRot;
            }

            var @class = UClass.GetClass("BGP_AIPlayerControllerB1"); // "BGPPlayerController" works for sure

            if (@class == null)
            {
                Logging.LogError("Class is null");
                return null;
            }

            var oldController = GameUtils.GetPlayerController();
            var newPawn = SpawnWukong(oldController, playerPawnClass, new FTransform(rot, loc), oldPawn);

            if (newPawn == null)
            {
                Logging.LogError("Failed to spawn new pawn");
                return null;
            }

            BackToOldPawn(oldController, oldPawn, newPawn);

            Logging.LogDebug("Assigned player {PlayerId} clone {CloneHash}", id, newPawn.GetEntityHash());

            var newControllerActor = GameUtils.GetWorld()?.SpawnActor(@class, ref loc, ref rot);
            if (newControllerActor != null && newControllerActor is BGP_AIPlayerControllerCS newController)
            {
                Logging.LogDebug("Spawned new controller");
                newController.Possess(newPawn);
            }

            // Reset falling timer.
            var events = BUS_EventCollectionCS.Get(newPawn);
            events.Evt_OnLeaveFalling.Invoke();
            events = BUS_EventCollectionCS.Get(oldPawn);
            events.Evt_OnLeaveFalling.Invoke();

            // get teamId
            var teamId = Constants.AvailableTeamIds.First();
            if (player.CustomProperties.TryGetValue(nameof(PlayerState.TeamId), out var assignedTeamId))
            {
                teamId = (int)assignedTeamId;
            }

            // get initial Hp and HpMax
            if (!player.CustomProperties.TryGetValue(nameof(PlayerState.Hp), out var initialHpObj) || initialHpObj is not float initialHp)
            {
                Logging.LogWarning("Joining player did not set initial HP");
                initialHp = 1000f;
            }
            else
            {
                Logging.LogDebug("Setting initial HP to {Hp}", initialHp);
            }

            if (!player.CustomProperties.TryGetValue($"{Constants.AttributePrefix}{EBGUAttrFloat.HpMaxBase}", out var initialHpMaxObj) || initialHpMaxObj is not float initialHpMaxBase)
            {
                Logging.LogWarning("Joining player did not set initial HPMax");
                initialHpMaxBase = 1000f;
            }
            else
            {
                Logging.LogDebug("Setting initial HPMax to {HpMax}", initialHpMaxBase);
            }

            var playerState = new PlayerState(id, newPawn, teamId, initialHp, initialHpMaxBase)
            {
                Location = loc,
                Rotation = rot
            };

            // set nickname
            if (player.CustomProperties.TryGetValue(ActorProperties.NickName, out var nickName))
            {
                playerState.NickName = (string)nickName;
            }

            // set attributes
            foreach (var attr in Constants.SyncedAttributes)
            {
                if (player.CustomProperties.TryGetValue($"{Constants.AttributePrefix}{attr}", out var value))
                {
                    Logging.LogTrace("Setting remote player initial attribute {Attribute} = {Value}", attr, value);
                    playerState.Attributes[attr] = (float)value;
                }
            }

            // update equipment
            if (player.CustomProperties.TryGetValue(nameof(PlayerState.Equipment), out var eq))
            {
                playerState.Equipment = (EquipmentState)eq;
                EquipmentHelpers.SetRemoteActorEquipment(newPawn, playerState.Equipment);
            }

            // set lock distance
            FUStUnitCommDesc unitCommDesc = BGW_GameDB.GetUnitCommDesc(newPawn.GetResID());
            if (unitCommDesc != null)
            {
                unitCommDesc.CameraLockDist = 10000;
            }

            return playerState;
        }

        private void CreateMarkerForCharacter(CharacterState characterState)
        {
            var world = GameUtils.GetWorld();
            var playerMarkerActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(Constants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);
            var playerMarkerActor = BGU_UnrealWorldUtil.SpawnActor(world, playerMarkerActorClass);
            if (playerMarkerActor != null)
            {
                Logging.LogDebug("Player marker actor spawned successfully");
            }
            else
            {
                Logging.LogError("Cannot spawn player marker actor");
                return;
            }

            var teamName = GameUtils.GetTeamName(characterState.TeamId);
            playerMarkerActor.CallFunctionByNameWithArguments($"SetText {characterState.NickName} {teamName}", true);
            characterState.MarkerActor = playerMarkerActor;
        }
    }
}