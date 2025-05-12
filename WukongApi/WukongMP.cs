using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using b1.ECS;
using B1UI.GSUI;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.API;
using WukongApi.ECS;
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

        public WukongClient Client { get; }

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
            Client = new WukongClient(OnBeforeJoinedRoomCallback, OnAfterJoinedRoomCallback, p => { GameLoopPatch.QueueOnGameThread(() => AddPlayer(p), "AddPlayer"); });
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

            ConfigureEventCallbacks();
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
            if (!Client.ConnectedAndInRoom)
            {
                GameUtils.DestroyAllTamers();
                BlueprintUiUtils.SpawnUiManagerActor();
                InitializeWidgets();
                Client.StartClient();
            }
        }

        private void OnEndPlay()
        {
            Logging.LogInformation("End play for player.");
            DeinitializeWidgets();
            Client.StopRelayClient();
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
            PingIndicatorWidget.Instance.Initialize();
            PingIndicatorWidget.Instance.SetVisibility(true);
            FreeCameraControlsWidget.Instance.Initialize();
        }

        private void DeinitializeWidgets()
        {
            ChatWidget.Instance.Deinitialize();
            _timerWidget.Deinitialize();
            _lobbyStatusWidget.Deinitialize();
            _gameMessageWidget.Deinitialize();
            _countdownWidget.Deinitialize();
            _infoMessageWidget.Deinitialize();
            PingIndicatorWidget.Instance.Deinitialize();
        }

        private void OnLoadingScreenClose()
        {
            if (Client is { RelayClient.InRoom: true })
            {
                ChatWidget.Instance.SetVisibility(true);
                _isAfterLoadingScreen = true;
                if (Client.RoomState.InMatchmaking)
                {
                    var timeDifference = new DateTime(Client.RoomState.MatchmakingEndTime, DateTimeKind.Utc) - DateTime.UtcNow;
                    _timerWidget.StartCountdown(0, timeDifference.Seconds, EndMatchmaking);
                    SetupMatchmakingUi();
                }
                else if (Client.LocalPlayerState.IsSpectator)
                {
                    HandleBecameSpectator(Client.LocalPlayerState); // TODO: Called twice?
                }
                else
                {
                    SetupLobbyUi();
                }

                UpdatePlayerTeamUi(Client.LocalPlayerState);
            }
        }

        public void HandleBecameSpectator(PlayerState playerState)
        {
            var isMyself = playerState.PeerId == Client.LocalPlayerState.PeerId;

            if (isMyself)
                SetHudVisibility(false);

            SetPlayerVisibility(playerState, false);

            if (isMyself)
            {
                FreeCameraManager.EnterFreeCameraMode();
                SetupSpectatorUi();
            }

            UpdatePlayerTeamUi(playerState);
        }

        public void HandleStoppedBeingSpectator(PlayerState playerState)
        {
            var isMyself = playerState.PeerId == Client.LocalPlayerState.PeerId;

            if (isMyself)
                SetHudVisibility(true);

            SetPlayerVisibility(playerState, true);

            if (isMyself)
            {
                FreeCameraManager.LeaveFreeCameraMode();
                if (Client.RoomState.InMatchmaking)
                {
                    SetupMatchmakingUi();
                }
                else if (!Client.RoomState.InPvP)
                {
                    SetupLobbyUi();
                }
                else
                {
                    _lobbyStatusWidget.SetVisibility(false);
                }
            }

            UpdatePlayerTeamUi(playerState);
        }

        private void SetupLobbyUi()
        {
            if (!_isAfterLoadingScreen)
                return;

            _gameMessageWidget.SetVisibility(true);
            _gameMessageWidget.SetMainText(Resources.Texts.InMultiplayer);
            _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(Client.ConnectedPlayers.Count, Client.LocalPlayerState.IsReadyForPvP));
            _gameMessageWidget.SetThirdText(Resources.Texts.PressToSwitchTeam);
            _lobbyStatusWidget.SetVisibility(true);
        }

        private void SetupMatchmakingUi()
        {
            if (!_isAfterLoadingScreen)
                return;

            _gameMessageWidget.SetVisibility(true);
            _gameMessageWidget.SetMainText(Resources.Texts.InMultiplayer);
            _gameMessageWidget.SetSecondText(Resources.Texts.MatchmakingInProgress);
            _gameMessageWidget.SetThirdText("");
            _lobbyStatusWidget.SetVisibility(true);
        }

        private void SetupSpectatorUi()
        {
            if (!_isAfterLoadingScreen)
                return;

            _gameMessageWidget.SetVisibility(true);
            _gameMessageWidget.SetMainText(Resources.Texts.InMultiplayer);
            _gameMessageWidget.SetSecondText(Resources.Texts.WaitForEnd);
            _gameMessageWidget.SetThirdText("");
            _lobbyStatusWidget.SetVisibility(true);
        }

        public void DumpDebugInfo()
        {
            // dump room state
            Logging.LogDebug("Room state: {State}", Client.RoomState.ToString());

            // dump player state to console for me
            Logging.LogDebug("Local player state: {State}", Client.LocalPlayerState.ToString());
            // dump player state to console for each connected player
            foreach (var (id, state) in Client.ConnectedPlayers)
            {
                Logging.LogDebug("Player {PlayerId} state: {State}", id, state.ToString());
            }

            // dump synced monsters
            Client.entityManager.RunSystem((EntityId entity, ref TamerComponent tamer, ref HpComponent hp, ref TeamComponent team) =>
            {
                var realTeamId = tamer.Tamer?.GetMonster().GetTeamIDInCS();
                Logging.LogDebug($"Monster [{entity}]: Guid={tamer.Guid}, TeamId={team.TeamId}, RealTeamId={realTeamId} Hp={hp.Hp}, IsSynced={tamer.IsSynced}, IsTamerValid={tamer.IsTamerValid}");
            });

            // print team hostility info
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            foreach (var (teamId, relation) in teamRelationData.TeamHostileInfos)
            {
                Logging.LogDebug("Team {TeamId} hostility: {HostileTeams}", teamId, string.Join(", ", relation.HostileTeamIDs));
            }
        }

        public bool ShouldRunConnectedPatches()
        {
            return Client is { ConnectedAndInRoom: true };
        }

        public void StartRound()
        {
            _timerWidget.StopCountdown();
            _gameMessageWidget.SetVisibility(false);
            _countdownWidget.StopCountdown();
            _timerWidget.StartCountdown(Constants.RoundMinutes, Constants.RoundSeconds, OnRoundEnded);
            if (Client.IsMasterClient)
            {
                Client.RoomState.InCombatRound = true;

                var monsterCount = 0;
                Client.entityManager.RunSystem((EntityId _, ref TamerComponent tamer) =>
                {
                    if (tamer.IsSynced)
                    {
                        monsterCount++;
                    }
                });

                if (Client.RoomState.BotsEnabled && Client.ConnectedPlayers.Count == 0 && monsterCount == 0)
                {
                    GameLoopPatch.QueueOnGameThread(SpawnBots, "SpawnBots");
                }
            }
        }

        private void OnRoundEnded()
        {
            Logging.LogInformation("Round time ended, ending round");
            if (Client.IsMasterClient)
            {
                Task.Run(async () => await Client.LobbyManager.EndRoundAsync(Constants.DrawTeamId));
            }
        }

        public void EndRound()
        {
            _timerWidget.StopCountdown();

            if (Client.IsMasterClient)
            {
                Client.RoomState.InCombatRound = false;
                foreach (var playerState in Client.AllConnectedPlayers)
                {
                    var events = BUS_EventCollectionCS.Get(playerState.Pawn);
                    events?.Evt_RelieveImmobilized.Invoke();
                    events?.Evt_RelievePhantomRush.Invoke();
                }
            }
        }

        public void ResetRoundState()
        {
            Utils.TryRunOnGameThread(DestroySyncedMonsters);
        }

        public void EnablePvP()
        {
            Logging.LogInformation("Enabled PvP");

            var myTeam = Client.LocalPlayerState.TeamId;
            var otherTeams = Client.ConnectedPlayers.Values
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

            var myTeam = Client.LocalPlayerState.TeamId;
            var otherTeams = Client.ConnectedPlayers.Values
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

        public void EndTournament(int winnerTeamId)
        {
            Logging.LogInformation("End tournament");
            SetupLobbyUi();
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
                    var hasGuid = false;

                    Client.entityManager.RunSystem((EntityId _, ref TamerComponent tamer) =>
                    {
                        if (tamer.Guid == guid)
                        {
                            hasGuid = true;
                        }
                    });

                    if (actor.GetMonster() == null)
                    {
                        Logging.LogDebug("Spawning monster for tamer with guid: {Guid}.", guid);

                        if (!hasGuid)
                        {
                            Logging.LogError("Not syncing monster");
                        }

                        Logging.LogDebug("Invoking Evt_TamerBlockingSpawnImmediately.");
                        events.Evt_TamerBlockingSpawnImmediately.Invoke(guid);
                    }
                    else if (!hasGuid)
                    {
                        Logging.LogDebug("Monster already spawned but not synced: {Guid}.", guid);

                        Logging.LogError("Not syncing monster");
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

            var player = Client.GetPlayerById(playerId);
            if (player == null)
                return;

            if (player.PeerId == Client.LocalPlayerState.PeerId)
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

        private void ConfigureEventCallbacks()
        {
            if (Client.ConnectedAndInRoom)
            {
                Logging.LogError("Relay client is already connected and ready");
                return;
            }

            Client.OnBeforeJoinRoom += SetPlayerProperties;
            Client.OnUnitSpawn += (_, id, guid, name, teamId, x, y, z) => GameLoopPatch.QueueOnGameThread(() => SpawnRemoteUnit(id, guid, name, teamId, x, y, z), "SpawnRemoteUnit");
            Client.OnSummonSpawn += (summonerId, id, guid, name, teamId) => GameLoopPatch.QueueOnGameThread(() => SpawnRemoteSummon(summonerId, id, guid, name, teamId), "SpawnRemoteSummon");
            Client.OnMontageCallback += (data) => GameLoopPatch.QueueOnGameThread(() => ApplyPlayerMontageCallback(data), "ApplyPlayerMontageCallback");
            Client.OnTeleportFinish += (id) => GameLoopPatch.QueueOnGameThread(() => OnTeleportFinish(id), "WakeUpMonster");
            Client.OnMonsterWakeUp += guid => GameLoopPatch.QueueOnGameThread(() => WakeUpMonster(guid), "WakeUpMonster");
            Client.OnEquipmentChange += (id, eq) => GameLoopPatch.QueueOnGameThread(() => ChangeEquipment(id, eq), "ChangeEquipment");
            Client.OnReadinessChange += (name, isReady, readyCount) => Utils.TryRunOnGameThread(() => UpdateReadiness(name, isReady, readyCount));
            Client.OnTeamChange += (playerState, teamId) => Utils.TryRunOnGameThread(() => UpdatePlayerTeam(playerState, teamId));
            Client.OnPlayerLeft += playerState => Utils.TryRunOnGameThread(() => RemovePlayer(playerState));
            Client.OnDamageNum += damageNum => GameLoopPatch.QueueOnGameThread(() => OnDamageNum(damageNum), "OnDamageNum", BGW_TickGroupMask.TG_PreAnim);
            Client.OnPlayerRebirth += id => GameLoopPatch.QueueOnGameThread(() => RebirthPlayer(id), "RebirthPlayer");
            Client.OnKillPlayer += id => GameLoopPatch.QueueOnGameThread(() => KillPlayer(id), "KillPlayer");
            Client.OnSetPlayerTransform += (loc, rot) => GameLoopPatch.QueueOnGameThread(() => TeleportLocalPlayer(loc, rot), "TeleportLocalPlayer");
            Client.OnPhantomRush += (id, direction) => GameLoopPatch.QueueOnGameThread(() => PerformPhantomRush(id, direction), "PerformPhantomRush");
            Client.OnExitPhantomRush += (id) => GameLoopPatch.QueueOnGameThread(() => ExitPhantomRush(id), "ExitPhantomRush");
            Client.OnHandleImmobilize += (id, otherId, type, hasBuff) => GameLoopPatch.QueueOnGameThread(() => HandleImmobilize(id, otherId, type, hasBuff), "HandleImmobilize");
            Client.OnTargetSet += (characterId, targetId, clear) => GameLoopPatch.QueueOnGameThread(() => OnTargetSet(characterId, targetId, clear), "OnTargetSet");
            Client.OnMatchmakingEnded += () => GameLoopPatch.QueueOnGameThread(OnMatchmakingEnded, "OnMatchmakingEnded");
            Client.OnBuffAdded += (playerId, buffId, duration) => GameLoopPatch.QueueOnGameThread(() => OnBuffAdded(playerId, buffId, duration), "OnBuffAdded");
            Client.OnBuffRemoved += (playerId, a, b, c, d) => GameLoopPatch.QueueOnGameThread(() => OnBuffRemoved(playerId, a, b, c, d), "OnBuffRemoved");
            Client.OnBuffAllRemoved += (playerId, a, b) => GameLoopPatch.QueueOnGameThread(() => OnBuffAllRemoved(playerId, a, b), "OnBuffAllRemoved");
            Client.OnStateTriggerSet += (characterId, trigger, time, isForce) => GameLoopPatch.QueueOnGameThread(() => OnStateTriggerSet(characterId, trigger, time, isForce), "OnStateTriggerSet");
            Client.OnSimpleStateSet += (characterId, state, isRemove) => GameLoopPatch.QueueOnGameThread(() => OnSimpleStateSet(characterId, state, isRemove), "OnSimpleStateSet");
            Client.OnFsmStateSet += (characterId, eventName) => GameLoopPatch.QueueOnGameThread(() => OnFsmStateSet(characterId, eventName), "OnFsmStateSet", BGW_TickGroupMask.TG_BeforeStartPhsic);
            Client.OnMotionMatchingChanged += (characterId, mm) => GameLoopPatch.QueueOnGameThread(() => OnMotionMatchingChanged(characterId, mm), "OnMotionMatchingChanged");
            Client.OnRequestSpawnUnits += (playerId, unitName, count, teamId) => GameLoopPatch.QueueOnGameThread(() => SpawnUnitsMaster(playerId, unitName, count, teamId), "SpawnUnitsMaster");
        }

        private void OnBuffAdded(int playerId, int buffId, float duration)
        {
            var playerState = Client.GetPlayerById(playerId);
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

            Logging.LogTrace("Adding buff {BuffId} to player {Nickname} with duration {Duration}", buffId, playerState.NickName, duration);
            events.Evt_BuffAdd.Invoke(buffId, playerState.Pawn, playerState.Pawn, duration);
        }

        private void OnBuffRemoved(int playerId, int buffId,
            EBuffEffectTriggerType removeTriggerType,
            int inLayer,
            bool withTriggerRemoveEffect)
        {
            var playerState = Client.GetPlayerById(playerId);
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

            Logging.LogTrace("Removing buff {BuffId} from player {Nickname}, type: {Type}", buffId, playerState.NickName, removeTriggerType);
            events.Evt_BuffRemove.Invoke(buffId, removeTriggerType, inLayer, withTriggerRemoveEffect);
        }

        private void OnBuffAllRemoved(int playerId, EBuffEffectTriggerType removeTriggerType, bool withTriggerRemoveEffect)
        {
            var playerState = Client.GetPlayerById(playerId);
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

            Logging.LogTrace("Removing all buffs from player {Nickname}, type: {Type}", playerState.NickName, removeTriggerType);
            events.Evt_BuffAllRemove.Invoke(removeTriggerType, withTriggerRemoveEffect);
        }

        private void OnStateTriggerSet(int characterId, EBUStateTrigger trigger, float time, bool needForceUpdate)
        {
            var pawn = Client.GetPawnByPeerId(characterId);
            if (pawn == null)
            {
                LogNullCharacter(characterId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for pawn {PathName}", pawn.PathName);
                return;
            }

            events.Evt_UnitStateTrigger.Invoke(trigger, time, needForceUpdate);
        }

        private void OnSimpleStateSet(int characterId, EBGUSimpleState state, bool isForce)
        {
            var pawn = Client.GetPawnByPeerId(characterId);
            if (pawn == null)
            {
                LogNullCharacter(characterId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for pawn {PathName}", pawn.PathName);
                return;
            }

            Logging.LogTrace("Setting simple state: {State}, with isRemove {Remove} for pawn {PathName}", state, isForce, pawn.PathName);
            events.Evt_UnitSetSimpleState.Invoke(state, isForce);
        }

        private void OnFsmStateSet(int characterId, string eventName)
        {
            var pawn = Client.GetPawnByPeerId(characterId);
            if (pawn == null)
            {
                LogNullCharacter(characterId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for character {Pawn}", pawn.PathName);
                return;
            }

            Logging.LogTrace("Triggering fsm event: {Event}, for player {Player}", eventName, pawn.PathName);
            events.Evt_TriggerFsmEvent.Invoke(eventName.MakeGameplayTag());
        }

        // TODO: System, this is not called anywhere
        private void OnMotionMatchingChanged(int characterId, EState_MM motionMatchingState)
        {
            var entity = Client.entityManager.GetEntityByPeerId(characterId);
            if (!entity.HasValue)
            {
                LogNullCharacter(characterId);
                return;
            }

            var tamerComponent = Client.GetEntityComponent<TamerComponent>(entity.Value);
            
            var events = BUS_EventCollectionCS.Get(tamerComponent.Pawn);
            
            if (events == null)
            {
                Logging.LogError("Failed to get event collection for pawn {PathName}", tamerComponent.Pawn!.PathName);
                return;
            }
            
            Logging.LogTrace("Changing motion matching to: {State}, for monster {Monster}", motionMatchingState, tamerComponent.Pawn!.PathName);
            events.Evt_ChangeMotionMatchingState.Invoke(motionMatchingState);
        }

        private void ExitPhantomRush(int playerId)
        {
            var playerState = Client.GetPlayerById(playerId);
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

        private void OnTargetSet(int playerId, int targetId, bool clearTarget)
        {
            var pawn = Client.GetPawnByPeerId(playerId);
            if (pawn == null)
            {
                LogNullCharacter(targetId);
                return;
            }

            var targetInfoData = (BUC_TargetInfoData)BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(pawn);
            if (clearTarget)
            {
                Logging.LogDebug("Updating target for pawn {Pawn} to null", pawn.PathName);
                targetInfoData.SetTargetInfo(new UnitLockTargetInfo());
                return;
            }

            var targetPawn = Client.GetPawnByPeerId(targetId);
            if (targetPawn == null)
            {
                LogNullCharacter(targetId);
                return;
            }

            Logging.LogDebug("Updating target for pawn {Pawn} to pawn {Pawn}", pawn.PathName, targetPawn.PathName);
            targetInfoData.SetTargetInfo(new UnitLockTargetInfo(targetPawn, ETargetSourceType.SkillBase_NormalUse));
        }

        private void UpdatePlayerTeam(PlayerState playerState, int teamId)
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
                var teamName = GameUtils.GetTeamName(playerState.TeamId);
                playerState.MarkerActor.CallFunctionByNameWithArguments($"SetText {playerState.NickName} {teamName}", true);
            }

            UpdatePlayerTeamUi(playerState);
        }

        private void UpdatePlayerTeamUi(PlayerState playerState)
        {
            _lobbyStatusWidget.UpdatePlayerTeam(playerState.NickName, playerState.TeamId, playerState.IsSpectator);
        }

        private void KillPlayer(int playerId)
        {
            var player = Client.GetPlayerById(playerId)?.Pawn;
            if (player == null)
                return;

            var events = BUS_EventCollectionCS.Get(player);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            if (Client.IsMasterClient)
            {
                events?.Evt_UnitDead.Invoke(player, EDeadReason.Suicide);
            }
        }

        private void TeleportLocalPlayer(FVector location, FRotator rotation)
        {
            var playerState = Client.LocalPlayerState;
            BUS_EventCollectionCS.Get(playerState.Pawn)?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportBegin, -1f);
            playerState.TeleportFinishFrames = 5;
            GameUtils.GetControlledPawn()?.SetActorTransform(new FTransform(rotation, location), false, out _, true);
            GameUtils.GetPlayerController().SetControlRotation(rotation);
        }

        private void PerformPhantomRush(int playerId, ESkillDirection direction)
        {
            var playerState = Client.GetPlayerById(playerId);
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
            var player = GameUtils.GetControlledPawn();

            if (player == null)
            {
                Logging.LogError("Failed to get player");
                return;
            }

            ResetCooldown(player);
            ResetMana(player);
        }

        public static void SetHudVisibility(bool visible)
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

        private void HandleImmobilize(int characterId, int otherCharacterId, ImmobilizeActionType immobilizeAction, bool hasBuff)
        {
            var pawn = Client.GetPawnByPeerId(characterId);
            if (pawn == null)
            {
                LogNullCharacter(characterId);
                return;
            }

            var otherCharacterState = Client.GetPawnByPeerId(otherCharacterId);

            switch (immobilizeAction)
            {
                case ImmobilizeActionType.Cast:
                    CastImmobilize(pawn);
                    break;
                case ImmobilizeActionType.Trigger:
                    if (otherCharacterState == null)
                    {
                        Logging.LogError("Player not found: {Id}", otherCharacterId);
                        return;
                    }

                    TriggerImmobilize(pawn, otherCharacterState, hasBuff);
                    break;
                case ImmobilizeActionType.Relieve:
                    RelieveImmobilize(pawn);
                    break;
                case ImmobilizeActionType.Break:
                // Currently not supported
                default:
                    Logging.LogError("Unknown ImmobilizeActionType: {Action}", immobilizeAction);
                    break;
            }
        }

        private void CastImmobilize(BGUCharacterCS castingCharacterState)
        {
            if (Client.IsMasterClient)
            {
                Logging.LogDebug("Received cast immobilize for character {Nickname}", castingCharacterState.GetName());
                var playerEvents = BUS_EventCollectionCS.Get(castingCharacterState);
                playerEvents.Evt_CastImmobilize.Invoke(0);
            }
        }

        private static void TriggerImmobilize(BGUCharacterCS? pawn, BGUCharacterCS? caster, bool hasBuff)
        {
            Logging.LogDebug("Received trigger immobilize for character {Pawn}", pawn?.GetName());

            if (pawn == null)
            {
                Logging.LogError("Failed to cast immobilizedCharacter to BGUCharacterCS");
                return;
            }

            if (caster == null)
            {
                Logging.LogError("Failed to cast castingCharacter to BGUCharacterCS");
                return;
            }

            var castImmobilizeData = (BUC_CastImmobilizeData)caster.GetDataByChunk(TypeManager.GetTypeIndex<BUC_CastImmobilizeData>());

            var cachedImmobilizeConfigDesc = castImmobilizeData.GetCachedImmobilizeConfigDesc(castImmobilizeData.ResId);
            if (cachedImmobilizeConfigDesc == null)
            {
                Logging.LogError("cachedImmobilizeConfigDesc is null");
                return;
            }

            var immobilizeConfigInstance = GameUtils.CreateImmobilizeConfig(pawn, caster, cachedImmobilizeConfigDesc, castImmobilizeData.ResId, hasBuff);
            BUS_EventCollectionCS.Get(pawn)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);
        }

        private static void RelieveImmobilize(BGUCharacterCS pawn)
        {
            Logging.LogDebug("Received relieve immobilize for player {Nickname}", pawn.GetName());
            var playerEvents = BUS_EventCollectionCS.Get(pawn);
            
            // TODO
            // pawn.RunImmobilizePatches = true;
            
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

            Client.CachePlayerProperty(nameof(PlayerState.Location), player.GetActorLocation());
            Client.CachePlayerProperty(nameof(PlayerState.Rotation), player.GetActorRotation());

            // nickname
            var nickname = CmdLineParams.Instance.Nickname;
            Client.CachePlayerProperty(nameof(PlayerState.NickName), nickname);

            // equipment
            var eq = EquipmentHelpers.GetCurrentEquipmentStateForActor(player);
            Client.CachePlayerProperty(nameof(PlayerState.Equipment), eq);

            // attributes
            var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(player);
            foreach (var attr in Constants.SyncedAttributes)
            {
                var value = attrs.GetFloatValue(attr);
                Client.CachePlayerAttribute(attr, value);
            }

            // hp
            var hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
            Client.CachePlayerProperty(nameof(PlayerState.Hp), hp);

            Client.SetCachedPlayerProperties();
            Logging.LogDebug("Finished setting initial player properties");
        }

        private void ChangeEquipment(int id, EquipmentState eq)
        {
            if (id == Client.LocalPlayerState.PeerId)
                return;

            if (!Client.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Logging.LogError("Player not found: {PlayerId}", id);
                return;
            }

            if (player.Pawn == null)
            {
                Logging.LogWarning("Failed to cast pawn to BGUCharacterCS");
                return;
            }

            EquipmentHelpers.SetRemoteActorEquipment(player.Pawn, eq);
        }

        private void UpdateReadiness(string playerNickName, bool isReady, int readyCount)
        {
            if (Client.IsMasterClient) // send this only once
            {
                if (isReady)
                {
                    Client.WukongChat.SendServerMessage("PlayerIsReady", playerNickName);
                }
                else
                {
                    Client.WukongChat.SendServerMessage("PlayerIsNotReady", playerNickName);
                }
            }

            if (isReady)
            {
                if ((Client.ConnectedPlayers.Count > 0 || Client.RoomState.BotsEnabled) && readyCount == Client.ConnectedPlayers.Count + 1)
                {
                    // all players are ready
                    _gameMessageWidget.SetMainText(Resources.Texts.StartingGame);
                    _countdownWidget.StartLobbyCountdown(Constants.CountdownSeconds, Client.StartPvP);
                }

                _lobbyStatusWidget.SetReadyCount(readyCount);
            }
            else
            {
                _countdownWidget.StopCountdown();
                _gameMessageWidget.SetMainText(Resources.Texts.InMultiplayer);
                _lobbyStatusWidget.SetReadyCount(readyCount);
            }
        }

        public void SwitchReadyState(bool isReady)
        {
            _gameMessageWidget.SetThirdText(isReady ? Resources.Texts.YouAreReady : Resources.Texts.PressToSwitchTeam);
            _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(Client.ConnectedPlayers.Count, isReady));
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

            _lobbyStatusWidget.RemovePlayerFromTeams(playerState.NickName);
            UpdateConnectedCount();
            _lobbyStatusWidget.SetReadyCount(Client.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
        }

        private void UpdateConnectedCount()
        {
            _lobbyStatusWidget.SetConnectedCount(Client.ConnectedPlayers.Count + 1);
            _gameMessageWidget.SetSecondText(TextUtils.GetReadyText(Client.ConnectedPlayers.Count, Client.LocalPlayerState.IsReadyForPvP));
        }

        private static void OnDamageNum(DamageNumParam damageNum)
        {
            var uiEvt = BGW_UIEventCollection.Get(GameUtils.GetWorld());
            uiEvt.Evt_UI_ShowHPChangeNum(damageNum);
        }

        public void ApplyPlayerMontageCallback(MontageCallbackData data)
        {
            var id = data.CharacterId;
            var pawn = Client.GetPawnByPeerId(id);
            if (pawn == null)
            {
                LogNullCharacter(id);
                return;
            }

            if (string.IsNullOrEmpty(data.MontagePath))
            {
                Logging.LogDebug("Stopping montage playback for character {CharacterId}", id);
                pawn.StopAnimMontage(null);
                return;
            }

            var fullMontagePath = data.Compressed ? MontageHelpers.DecompressMontageName(data.MontagePath) : data.MontagePath;
            Logging.LogDebug("Received montage: {Montage}, position: {Position}, reset: {Reset}", fullMontagePath, data.Position, data.Reset);

            var animInstance = pawn.Mesh.GetAnimInstance();
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

            var events = BUS_EventCollectionCS.Get(pawn);

            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            Logging.LogDebug("Applying montage callback for character {CharacterId} with montage {Montage} @ {Position}", id, fullMontagePath, data.Position);
            animInstance.Montage_Play(montage, 1f, EMontagePlayReturnType.MontageLength, data.Position);
            events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
        }

        public void SpawnUnitsMaster(int spawningPlayerId, string unitName, int count, int teamId)
        {
            var playerState = Client.GetPlayerById(spawningPlayerId);
            if (playerState == null || playerState.Pawn == null)
            {
                Logging.LogError("Player not found: {PlayerId}", spawningPlayerId);
                return;
            }

            var spawnLoc = playerState.Pawn.GetActorLocation() + playerState.Pawn.GetActorForwardVector() * Constants.MonsterSpawnDistance;
            var startLoc = spawnLoc + FVector.UpVector * Constants.MonsterSpawnTraceHeight / 2;
            var endLoc = spawnLoc - FVector.UpVector * Constants.MonsterSpawnTraceHeight / 2;

            // trace vertically for spawn height
            var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), startLoc, endLoc, out var hitResultSimple);
            if (hit)
            {
                spawnLoc = hitResultSimple.HitLocation + FVector.UpVector * Constants.MonsterHalfHeight;
            }

            // spawn in a grid around center point, separated by 200 units
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((float)count / cols);

            float startX = -((cols - 1) * Constants.MonsterSpawnSpread) / 2f;
            float startY = -((rows - 1) * Constants.MonsterSpawnSpread) / 2f;

            int placed = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    float x = startX + col * Constants.MonsterSpawnSpread;
                    float y = startY + row * Constants.MonsterSpawnSpread;
                    var loc = spawnLoc + new FVector(x, y, 0);

                    var localI = placed;
                    Task.Run(async () =>
                    {
                        // wait for i * 200ms
                        await Task.Delay(localI * Constants.MonsterSpawnDelayMs);
                        GameLoopPatch.QueueOnGameThread(() => { SpawnUnitMaster(unitName, loc, teamId); }, "SpawnUnitMaster");
                    });
                    placed++;
                    if (placed == count)
                        goto Notify;
                }
            }

            Notify:
            Client.WukongChat.SendServerMessage("PlayerSpawned", Client.LocalPlayerState.NickName, count.ToString(), unitName);
        }

        private void SpawnUnitMaster(string unitName, FVector loc, int teamId)
        {
            var unitPath = UnitPathsConfig.GetUnitPath(unitName);

            var guid = Guid.NewGuid().ToString();
            var id = --Client.RoomState.NextMonsterId;

            Logging.LogDebug("Sending spawn unit {Name} at {Location}", unitName, loc.ToCompactString());
            Client.SpawnUnit(id, guid, unitPath, teamId, loc.X, loc.Y, loc.Z);

            SpawnUnitLocally(id, guid, unitPath, teamId, loc.X, loc.Y, loc.Z);
        }

        private void SpawnRemoteUnit(int id, string guid, string unitName, int teamId, float x, float y, float z)
        {
            SpawnUnitLocally(id, guid, unitName, teamId, x, y, z);
        }

        private void SpawnRemoteSummon(int summonerId, int id, string guid, string unitName, int teamId)
        {
            SummonPatch.ExecuteSummon(summonerId, id, guid, unitName, teamId);
        }

        private void SpawnUnitLocally(int peerId, string guid, string unitPath, int teamId, float x, float y, float z)
        {
            Logging.LogDebug("Spawn unit called for {UnitPath}", unitPath);

            if (string.IsNullOrEmpty(unitPath))
                return;

            var loc = new FVector(x, y, z);
            var rot = new FRotator();

            var world = GameUtils.GetWorld();

            var unitClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitPath, ELoadResourceType.SyncLoadAndCache);
            var transform = new FTransform(rot, loc);
            var tamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, (TSubclassOf<AActor>)unitClass, transform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as BUTamerActor;
            if (tamerActor == null)
            {
                Logging.LogError("Could not spawn unit: {UnitPath}", unitPath);
                return;
            }

            tamerActor.MarkAsSpawnedTamer(null);
            tamerActor.ExtendConfigComp.ActorResetType = EBGUResetType.Destroy;

            tamerActor.SpawnedTamerGuid = guid;
            // Update final guid
            tamerActor.GetFinalGuid(true);

            Logging.LogDebug("Spawned enemy: {TamerName}, with Guid {Guid}", tamerActor.GetName(), guid);
            var entity = CreateMonster(peerId, guid, tamerActor, teamId, unitPath);

            ref var trans = ref Client.GetEntityComponent<TranslationComponent>(entity);
            trans.Position = loc.ToVector3();
            trans.Rotation = rot.ToVector3();

            UBGUFunctionLibrary.BGUFinishSpawningActor(tamerActor, transform);
            BGS_GSEventCollection.Get(tamerActor)?.Evt_TamerBlockingSpawnImmediately.Invoke(guid);

            ref var nameComp = ref Client.GetEntityComponent<NicknameComponent>(entity);
            nameComp.Nickname = "Bot";

            CreateMarkerForCharacter(entity); // 3D marker above monster
            if (unitPath == UnitPathsConfig.GetUnitPath(CharacterKind.Monkey))
            {
                SetMonkeyBotConfig(tamerActor.GetMonster());
            }
        }

        public EntityId CreateMonster(int peerId, string guid, BUTamerActor tamer, int teamId, string unitName)
        {
            var id = Client.RegisterMonster();

            ref var netIdComp = ref Client.GetEntityComponent<PeerIdComponent>(id);
            netIdComp.PeerId = peerId;

            Client.entityManager.AssociatePeerIdWithEntity(peerId, id);

            ref var tamerComp = ref Client.GetEntityComponent<TamerComponent>(id);
            tamerComp.Tamer = tamer;
            tamerComp.Guid = guid;
            tamerComp.UnitName = unitName;

            ref var teamComp = ref Client.GetEntityComponent<TeamComponent>(id);
            teamComp.TeamId = teamId;

            Logging.LogDebug("Created monster state with team ID: {TeamId} (assigned)", teamId);
            return id;
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

        private void SpawnBots()
        {
            for (int i = 0; i < Constants.BotCount; i++)
            {
                float angle = i / (float)Constants.BotCount * 2f * FMath.PI;
                float x = FMath.Cos(angle) * Constants.PvpMonsterRadius;
                float y = FMath.Sin(angle) * Constants.PvpMonsterRadius;

                var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
                FVector spawnPosition = levelData.PvpStartingLocation + new FVector(x, y, 0f);
                SpawnUnitMaster(CharacterKind.Monkey, spawnPosition, GameUtils.GetOppositeTeam(Client.LocalPlayerState.TeamId));
            }
        }

        public void DestroySyncedMonsters()
        {
            var entities = Client.entityManager.GetArchetype(Client.monsterArchetype)!;
            foreach (var entityId in entities.Entities.ToArray())
            {
                Client.entityManager.DestroyEntity(entityId);
            }
        }

        public void DestroyMonster(EntityId entity)
        {
            var tamerComp = Client.GetEntityComponent<TamerComponent>(entity);

            if (tamerComp.Tamer == null)
            {
                return;
            }

            var monsterPawn = tamerComp.Tamer.GetMonster();
            if (monsterPawn != null)
            {
                var events = BUS_EventCollectionCS.Get(monsterPawn);
                events.Evt_UnitDead.Invoke(null, EDeadReason.OnlyDestroyUnit);
                BGU_UnrealWorldUtil.DestroyActor(tamerComp.Pawn);
            }

            BGU_UnrealWorldUtil.DestroyActor(tamerComp.Tamer);

            CleanupMonster(entity);
        }

        public void CleanupMonster(EntityId entity)
        {
            var markerComp = Client.GetEntityComponent<MarkerComponent>(entity);

            if (markerComp.MarkerActor != null)
            {
                BGU_UnrealWorldUtil.DestroyActor(markerComp.MarkerActor);
            }

            Client.entityManager.DestroyEntity(entity);
        }

        private void OnBeforeJoinedRoomCallback()
        {
            SetUpRoom();
            SpawnPlayersAlreadyInRoom();
            UpdateConnectedCount();
            DisablePlayerSkills();
            _lobbyStatusWidget.SetReadyCount(Client.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
            _lobbyStatusWidget.SetMaxConnectedCount(Client.RoomState.MaxPlayers);
            SetupMatchmaking();
        }

        private void OnAfterJoinedRoomCallback()
        {
            var spawnPosition = GetSpawnPosition(Client.LocalPlayerState.PeerId);
            if (!Constants.IsCoop)
            {
                TeleportLocalPlayer(spawnPosition, FRotator.ZeroRotator);
            }
        }

        private void OnTeleportFinish(int playerId)
        {
            var playerState = Client.GetPlayerById(playerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {PlayerId}", playerId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);
            events?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportEnd, -1f);
            events?.Evt_TeleportFinish.Invoke();
        }

        public void UpdatePlayer(PlayerState playerState, float deltaTime)
        {
            playerState.UpdateMarkerPosition();

            if (playerState.TeleportFinishFrames >= 0)
            {
                if (playerState.TeleportFinishFrames == 0)
                {
                    Client.SendTeleportFinish();
                }

                playerState.TeleportFinishFrames--;
            }
        }

        private FVector GetSpawnPosition(int playerId)
        {
            int maxPlayersCount = Client.RoomState.MaxPlayers;

            float angle = playerId / (float)maxPlayersCount * 2f * FMath.PI;
            float x = FMath.Cos(angle) * Constants.PvpStartingRadius;
            float y = FMath.Sin(angle) * Constants.PvpStartingRadius;

            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            var baseLocation = levelData.PvpStartingLocation + new FVector(x, y, 0f);
            return GameUtils.GetFinalLocation(Client.GetPlayerById(playerId)?.Pawn, baseLocation);
        }

        private void SetUpRoom()
        {
            if (Client.IsMasterClient)
            {
                Client.RoomState.InPvP = false;
            }
        }

        private void SetupMatchmaking()
        {
            if (Client.RoomState.GameMode == GameMode.Private)
                return;

            if (Client.IsMasterClient)
            {
                Client.RoomState.InMatchmaking = true;
                Client.RoomState.MatchmakingEndTime = DateTime.UtcNow.AddSeconds(Constants.MatchmakingSeconds).Ticks;
            }
        }

        private void EndMatchmaking()
        {
            if (Client.IsMasterClient)
            {
                Client.RoomState.InMatchmaking = false;
                Client.SendEndMatchmaking();
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
            var player = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(player);
            if (events != null)
            {
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInVigorSkill);
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantCastFaBao);
            }
        }

        private void DisablePlayerInteraction()
        {
            var player = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(player);
            if (events != null)
            {
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInteract);
            }
        }

        private void SpawnPlayersAlreadyInRoom()
        {
            // when joining game, spawn all players already in room
            foreach (var player in Client.GetOtherPlayersInRoom())
            {
                GameLoopPatch.QueueOnGameThread(() => AddPlayer(player.PeerId), "AddPlayer");
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

        private void AddPlayer(int playerId)
        {
            var playerState = SpawnCloneForPlayer(playerId);

            if (playerState != null)
            {
                CreateMarkerForCharacter(playerState); // 3D marker above player
                Client.RegisterPlayer(playerState);
                UpdateConnectedCount();

                var props = Client.RelayClient.GetPlayerState(playerId)?.Properties;

                if (props == null)
                {
                    Logging.LogError("Player properties are null");
                    return;
                }

                // set IsSpectator if client should be (joining during fight)
                var isSpectator = playerState.IsSpectator;

                if (!isSpectator)
                {
                    isSpectator = Client.RoomState.InPvP && !playerState.IsReadyForPvP;
                }

                // set remote player property - IsSpectator
                if (Client.IsMasterClient)
                {
                    Client.SetRemotePlayerProperty(playerId, nameof(PlayerState.IsSpectator), isSpectator);
                }

                // readiness callback
                if (playerState.IsReadyForPvP)
                {
                    Client.OnPlayerReadinessChanged(playerState.NickName, playerState.IsReadyForPvP);
                }

                UpdatePlayerTeamUi(playerState);

                if (Client.AllConnectedPlayers.Count() == Client.RoomState.MaxPlayers)
                {
                    EndMatchmaking();
                }
            }
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

        private PlayerState? SpawnCloneForPlayer(int id)
        {
            if (Client.ConnectedPlayers.ContainsKey(id))
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

            var initialProps = Client.RelayClient.GetPlayerState(id)?.Properties;

            if (initialProps == null)
            {
                Logging.LogError("Player properties are null at player joining");
                return null;
            }

            if (initialProps.TryGetValue(nameof(PlayerState.Location), out var playerLoc))
            {
                loc = (FVector)playerLoc;
            }

            if (initialProps.TryGetValue(nameof(PlayerState.Rotation), out var playerRot))
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
            if (initialProps.TryGetValue(nameof(PlayerState.TeamId), out var assignedTeamId))
            {
                teamId = (int)assignedTeamId;
            }

            // get initial Hp and HpMax
            if (!initialProps.TryGetValue(nameof(PlayerState.Hp), out var initialHpObj) || initialHpObj is not float initialHp)
            {
                Logging.LogWarning("Joining player did not set initial HP");
                initialHp = 1000f;
            }
            else
            {
                Logging.LogDebug("Setting initial HP to {Hp}", initialHp);
            }

            if (!initialProps.TryGetValue($"{Constants.AttributePrefix}{EBGUAttrFloat.HpMaxBase}", out var initialHpMaxObj) || initialHpMaxObj is not float initialHpMaxBase)
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
            if (initialProps.TryGetValue(nameof(PlayerState.NickName), out var nickName))
            {
                playerState.NickName = (string)nickName;
                Logging.LogDebug("Setting initial Nickname to {Nickname}", playerState.NickName);
            }
            else
            {
                Logging.LogWarning("Initial nickname not provided");
            }

            // set IsReadyForPvP and IsSpectator
            if (initialProps.TryGetValue(nameof(PlayerState.IsReadyForPvP), out var isReady))
            {
                playerState.IsReadyForPvP = (bool)isReady;
                Logging.LogDebug("Setting initial IsReadyForPvP to {IsReady}", playerState.IsReadyForPvP);
            }

            if (initialProps.TryGetValue(nameof(PlayerState.IsSpectator), out var isSpectator))
            {
                playerState.IsSpectator = (bool)isSpectator;
                Logging.LogDebug("Setting initial IsSpectator to {IsSpectator}", playerState.IsSpectator);
            }

            // set attributes
            foreach (var attr in Constants.SyncedAttributes)
            {
                if (initialProps.TryGetValue($"{Constants.AttributePrefix}{attr}", out var value))
                {
                    Logging.LogTrace("Setting remote player initial attribute {Attribute} = {Value}", attr, value);
                    playerState.Attributes[attr] = (float)value;
                }
            }

            // update equipment
            if (initialProps.TryGetValue(nameof(PlayerState.Equipment), out var eq))
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

        private void CreateMarkerForCharacter(EntityId entity)
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

            var teamIdComp = Client.GetEntityComponent<TeamComponent>(entity);
            var nameComp = Client.GetEntityComponent<NicknameComponent>(entity);
            ref var markerComp = ref Client.GetEntityComponent<MarkerComponent>(entity);

            var teamName = GameUtils.GetTeamName(teamIdComp.TeamId);
            playerMarkerActor.CallFunctionByNameWithArguments($"SetText {nameComp.Nickname} {teamName}", true);
            markerComp.MarkerActor = playerMarkerActor;
        }

        [Obsolete]
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

        // TODO: This should always be error, make sure that creation and destruction of monsters is synchronized
        private void LogNullCharacter(int characterId)
        {
            if (characterId < 0)
                Logging.LogWarning("Character not found: {Id}", characterId); // monster not found
            else
                Logging.LogError("Character not found: {Id}", characterId); // player not found
        }
    }
}