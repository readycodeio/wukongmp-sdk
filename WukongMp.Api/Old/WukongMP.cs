using System;
using System.Linq;
using System.Threading.Tasks;
using b1;
using B1UI.GSUI;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
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
using Entity = Friflo.Engine.ECS.Entity;
using PlayerState = WukongMp.Api.Old.State.PlayerState;

namespace WukongMp.Api.Old
{
    // ReSharper disable once InconsistentNaming
    public class WukongMP
    {
        private readonly Harmony _harmony = new("WukongMP");

        public WukongClient Client { get; }

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
            Client.StopRelayClient();
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
                if (!Constants.IsCoop)
                {
                    GameUtils.DestroyAllTamers();
                }

                BlueprintUiUtils.SpawnUiManagerActor();
                InitializeWidgets();
                Client.StartClient();
            }
        }

        public void Reload()
        {
            OnMapLoaded();
            OnDelayBeginPlay();
            OnLoadingScreenClose();
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
            TimerWidget.Instance.Initialize();
            LobbyStatusWidget.Instance.Initialize();
            LobbyStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
            CoopStatusWidget.Instance.Initialize();
            CoopStatusWidget.Instance.SetMaxConnectedCount(Constants.MaxPlayers);
            GameMessageWidget.Instance.Initialize();
            CountdownWidget.Instance.Initialize();
            InfoMessageWidget.Instance.Initialize();
            PingIndicatorWidget.Instance.Initialize();
            PingIndicatorWidget.Instance.SetVisibility(true);
            FreeCameraControlsWidget.Instance.Initialize();
        }

        private void DeinitializeWidgets()
        {
            ChatWidget.Instance.Deinitialize();
            TimerWidget.Instance.Deinitialize();
            LobbyStatusWidget.Instance.Deinitialize();
            CoopStatusWidget.Instance.Deinitialize();
            GameMessageWidget.Instance.Deinitialize();
            CountdownWidget.Instance.Deinitialize();
            InfoMessageWidget.Instance.Deinitialize();
            PingIndicatorWidget.Instance.Deinitialize();
        }

        private void OnLoadingScreenClose()
        {
            if (Client is { RelayClient.InRoom: true })
            {
                ChatWidget.Instance.SetVisibility(true);
                PvPUtils.IsAfterLoadingScreen = true;
                if (Client.RoomState.InMatchmaking)
                {
                    var timeDifference = new DateTime(Client.RoomState.MatchmakingEndTime, DateTimeKind.Utc) - DateTime.UtcNow;
                    TimerWidget.Instance.StartCountdown(0, timeDifference.Seconds, EndMatchmaking);
                    PvPUtils.SetupMatchmakingUi();
                }
                else if (Client.LocalPlayerState.IsSpectator)
                {
                    HandleBecameSpectator(Client.LocalPlayerState); // TODO: Called twice?
                }
                else
                {
                    PvPUtils.SetupLobbyUi();
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
                FreeCameraManager.Instance.EnterFreeCameraMode();
                PvPUtils.SetupSpectatorUi();
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
                FreeCameraManager.Instance.LeaveFreeCameraMode();
                if (Client.RoomState.InMatchmaking)
                {
                    PvPUtils.SetupMatchmakingUi();
                }
                else if (!Client.RoomState.InPvP)
                {
                    PvPUtils.SetupLobbyUi();
                }
                else
                {
                    LobbyStatusWidget.Instance.SetVisibility(false);
                    CoopStatusWidget.Instance.SetVisibility(false);
                }
            }

            UpdatePlayerTeamUi(playerState);
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
            WukongMpMod.Instance.World.Query<NetworkIdComponent>().ForEachEntity((ref netId, entity) =>
            {
                Logging.LogDebug("Monster {Entity}: {NetId}", entity, netId);
                // TODO: Dump all monster info without using .DebugJson (throws due to some internal errors,
                // probably the same reason why JsonSerializer sometimes fails.
            });

            // print team hostility info
            var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

            foreach (var (teamId, relation) in teamRelationData.TeamHostileInfos)
            {
                Logging.LogDebug("Team {TeamId} hostility: {HostileTeams}", teamId, string.Join(", ", relation.HostileTeamIDs));
            }

            // dump perf info
            var perf = WukongMpMod.Instance.World.SystemRoot.GetPerfLog();
            if (perf != null)
            {
                Logging.LogDebug("Perf log:\n{Log}", perf);
            }
            else
            {
                Logging.LogDebug("Perf log is null");
            }
        }

        public bool ArePlayersCloseToSyncCutscene()
        {
            var LocalPlayerPosition = Client.LocalPlayerState.Pawn?.GetActorLocation() ?? FVector.ZeroVector;
            var squaredDistance = Constants.CutsceneSyncDistance * Constants.CutsceneSyncDistance;
            foreach (var actor in Client.AllConnectedPlayers)
            {
                if (actor.Pawn == null)
                    continue;

                if (actor.Pawn.GetActorLocation().Vector_DistanceSquared(LocalPlayerPosition) > squaredDistance)
                {
                    Logging.LogDebug("Player {Name} is too far away from local player", actor.NickName);
                    return false;
                }
            }

            return true;
        }

        public bool AreAllPlayersWaitingForMovie(int sequenceId)
        {
            return Client.AllConnectedPlayers.All(p => p.WaitingSequenceId == sequenceId);
        }

        public void SkipCutscene()
        {
            BGUFunctionLibraryCS.SkipCurrentSequence(GameUtils.GetWorld());
        }

        public bool ShouldRunConnectedPatches()
        {
            return Client is { ConnectedAndInRoom: true };
        }

        public void StartRound()
        {
            TimerWidget.Instance.StopCountdown();
            GameMessageWidget.Instance.SetVisibility(false);
            CountdownWidget.Instance.StopCountdown();
            TimerWidget.Instance.StartCountdown(Constants.RoundMinutes, Constants.RoundSeconds, OnRoundEnded);
            if (Client.IsMasterClient)
            {
                Client.RoomState.InCombatRound = true;

                var monsterCount = 0;
                WukongMpMod.Instance.World.Query<LocalTamerComponent>().ForEachEntity((ref tamer, _) =>
                {
                    if (tamer.IsSynced)
                    {
                        monsterCount++;
                    }
                });

                if (Client.RoomState.BotsEnabled && Client.ConnectedPlayers.Count == 0 && monsterCount == 0)
                {
                    GameLoopPatch.QueueOnGameThread(SpawningUtils.SpawnBots, "SpawnBots");
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
            TimerWidget.Instance.StopCountdown();

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
            Utils.TryRunOnGameThread(ClearEcsMonsters);
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

        private void ConfigureEventCallbacks()
        {
            if (Client.ConnectedAndInRoom)
            {
                Logging.LogError("Relay client is already connected and ready");
                return;
            }

            Client.OnBeforeJoinRoom += SetPlayerProperties;
            Client.OnEquipmentChange += (id, eq) => GameLoopPatch.QueueOnGameThread(() => ChangeEquipment(id, eq), "ChangeEquipment");
            Client.OnReadinessChange += (name, isReady, readyCount) => GameLoopPatch.QueueOnGameThread(() => UpdateReadiness(name, isReady, readyCount));
            Client.OnTeamChange += (playerState, teamId) => GameLoopPatch.QueueOnGameThread(() => UpdatePlayerTeam(playerState, teamId));
            Client.OnPlayerLeft += playerState => GameLoopPatch.QueueOnGameThread(() => RemovePlayer(playerState));
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
            if (Constants.IsCoop)
            {
                CoopStatusWidget.Instance.RemovePlayer(playerState.NickName);
                CoopStatusWidget.Instance.AddPlayer(playerState.NickName);
            }
            else
            {
                LobbyStatusWidget.Instance.UpdatePlayerTeam(playerState.NickName, playerState.TeamId, playerState.IsSpectator);
            }
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

        public static void ResetCooldown(APawn playerPawn)
        {
            var events = BUS_EventCollectionCS.Get(playerPawn);
            events?.Evt_ResetSkillCD.Invoke();
        }

        public static void ResetMana(APawn playerPawn)
        {
            var events = BUS_EventCollectionCS.Get(playerPawn);
            var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(playerPawn);
            float maxMana = attrContainer.GetFloatValue(EBGUAttrFloat.MpMax);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.Mp, maxMana);
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

        private void ChangeEquipment(short peerId, EquipmentState eq)
        {
            if (peerId == Client.LocalPlayerState.PeerId)
                return;

            if (!Client.ConnectedPlayers.TryGetValue(peerId, out var player))
            {
                Logging.LogError("Player not found: {PlayerId}", peerId);
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
                    GameMessageWidget.Instance.SetMainText(Texts.StartingGame);
                    CountdownWidget.Instance.StartLobbyCountdown(Constants.CountdownSeconds, Client.StartPvP);
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

        public void SwitchReadyState(bool isReady)
        {
            GameMessageWidget.Instance.SetThirdText(isReady ? Texts.YouAreReady : Texts.PressToSwitchTeam);
            GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(Client.ConnectedPlayers.Count, isReady));
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

            LobbyStatusWidget.Instance.RemovePlayerFromTeams(playerState.NickName);
            UpdateConnectedCount();
            LobbyStatusWidget.Instance.SetReadyCount(Client.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
            CoopStatusWidget.Instance.RemovePlayer(playerState.NickName);
        }

        private void UpdateConnectedCount()
        {
            LobbyStatusWidget.Instance.SetConnectedCount(Client.ConnectedPlayers.Count + 1);
            CoopStatusWidget.Instance.SetConnectedCount(Client.ConnectedPlayers.Count + 1);
            GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(Client.ConnectedPlayers.Count, Client.LocalPlayerState.IsReadyForPvP));
        }

        public void DiscoverMonsters()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            if (Client.IsMasterClient)
            {
                foreach (var actor in allActorsOfClass)
                {
                    var tamerRef = actor.CurrentRef;
                    Logging.LogDebug("Monster: {Name}, alive: {Flag}, phase {Phase}, type {Type}, guid: {Guid}", actor.GetName(), actor.GetMonster() != null, tamerRef.Phase, tamerRef.TamerType, BGU_DataUtil.GetActorGuid(actor));
                    if (tamerRef.Phase != ETamerPhase.Dead)
                    {
                        SpawningUtils.CreateMonsterInEcs(BGU_DataUtil.GetActorGuid(actor), actor, 2, actor.PathName);
                    }
                }
            }
        }

        public void ClearEcsMonsters()
        {
            WukongMpMod.Instance.World.Query<LocalTamerComponent>().ForEachEntity((ref _, entity) => { WukongMpMod.Instance.CommandBuffer.DeleteEntity(entity.Id); });
        }

        public void DestroyMonster(Entity entity)
        {
            var tamerComp = entity.GetComponent<LocalTamerComponent>();

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

        public void CleanupMonster(Entity entity)
        {
            var markerComp = entity.GetComponent<MarkerComponent>();

            if (markerComp.MarkerActor != null)
            {
                BGU_UnrealWorldUtil.DestroyActor(markerComp.MarkerActor);
            }

            Logging.LogDebug("Deleting entity from ECS: {Entity} (UnitDead)", entity.ToString());
            WukongMpMod.Instance.CommandBuffer.DeleteEntity(entity.Id);
        }

        private void OnBeforeJoinedRoomCallback()
        {
            SetUpRoom();
            SpawnPlayersAlreadyInRoom();
            UpdateConnectedCount();
            DisablePlayerSkills();
            LobbyStatusWidget.Instance.SetReadyCount(Client.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
            CoopStatusWidget.Instance.SetConnectedCount(Client.AllConnectedPlayers.Count());
            LobbyStatusWidget.Instance.SetMaxConnectedCount(Client.RoomState.MaxPlayers);
            CoopStatusWidget.Instance.SetMaxConnectedCount(Client.RoomState.MaxPlayers);
            SetupMatchmaking();
        }

        private void OnAfterJoinedRoomCallback()
        {
            if (!Constants.IsCoop)
            {
                var spawnPosition = GetSpawnPosition(Client.LocalPlayerState.PeerId);
                var data = new PlayerTransformData(Client.LocalPlayerState.PeerId, spawnPosition, FRotator.ZeroRotator);
                WukongMpMod.Instance.OnBroadcastPlayerTransform(data);
            }
            else
            {
                Utils.TryRunOnGameThread(DiscoverMonsters);
            }
        }

        public void UpdatePlayer(PlayerState playerState, float deltaTime)
        {
            playerState.UpdateMarkerPosition();

            if (playerState.TeleportFinishFrames >= 0)
            {
                if (playerState.TeleportFinishFrames == 0)
                {
                    WukongMpMod.Instance.SendTeleportFinish();
                }

                playerState.TeleportFinishFrames--;
            }
        }

        private FVector GetSpawnPosition(short peerId)
        {
            int maxPlayersCount = Client.RoomState.MaxPlayers;

            float angle = peerId / (float)maxPlayersCount * 2f * FMath.PI;
            float x = FMath.Cos(angle) * Constants.PvpStartingRadius;
            float y = FMath.Sin(angle) * Constants.PvpStartingRadius;

            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            var baseLocation = levelData.PvpStartingLocation + new FVector(x, y, 0f);
            return GameUtils.GetFinalLocation(Client.GetPlayerById(peerId)?.Pawn, baseLocation);
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
                WukongMpMod.Instance.SendEndMatchmaking();
            }

            TimerWidget.Instance.StopCountdown();
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

        private void AddPlayer(short peerId)
        {
            var playerState = SpawningUtils.SpawnCloneForPlayer(peerId);

            if (playerState != null)
            {
                MarkerUtils.CreateMarkerForCharacter(playerState); // 3D marker above player
                Client.RegisterPlayer(playerState);
                UpdateConnectedCount();

                var props = Client.RelayClient.GetPlayerState(peerId)?.Properties;

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
                    Client.SetRemotePlayerProperty(peerId, nameof(PlayerState.IsSpectator), isSpectator);
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
    }
}