using b1;
using b1.Plugins.AsyncLoadingScreen;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private List<AActor> _debugActors = new();

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
                if (!Constants.IsCoop)
                {
                    _harmony.PatchCategory(Constants.PvpPatches);
                }
                else
                {
                    _harmony.PatchCategory(Constants.CoopPatches);
                }
                Logging.LogInformation("Patched with Harmony");
            });
        }

        public void Unpatch()
        {
            Utils.TryRunOnGameThread(() =>
            {
                _harmony.UnpatchCategory(Constants.ConnectedPatches);
                _harmony.UnpatchCategory(Constants.GlobalPatches);
                if (!Constants.IsCoop)
                {
                    _harmony.UnpatchCategory(Constants.PvpPatches);
                }
                else
                {
                    _harmony.UnpatchCategory(Constants.CoopPatches);
                }
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
        }

        public void DeInit()
        {
            Client.StopRelayClient();
            IsInitialized = false;
        }

        public void OnDelayBeginPlay()
        {
            // this is triggered for every player controller, but we want to apply the logic once
            if (!Client.ConnectedAndInRoom)
            {
                if (!Constants.IsCoop)
                {
                    TamerUtils.DestroyAllTamers();
                }

                Logging.LogInformation("Initializing widgets");
                ModWidgetsUtils.SpawnWidgetManagerActor();
                ModWidgetsUtils.InitializeWidgets();
                Client.StartClient();
            }
        }

        public void Reload()
        {
            OnDelayBeginPlay();
            OnLoadingScreenClose();
        }

        public void OnEndPlay()
        {
            Client.StopRelayClient();
            Logging.LogInformation("Deinitializing widgets");
            ModWidgetsUtils.DeinitializeWidgets();
        }

        public void OnLoadingScreenClose()
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
                UIUtils.SetHudVisibility(false);

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
                UIUtils.SetHudVisibility(true);

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

        public void TestLevelTeleport()
        {
            var playerCharacter = GameUtils.GetControlledPawn();
            var teleportParam = new TeleportParam_Dream
            {
                LevelId = 20,
                TeleportPointName = new FName(""),
                RebirthPointId = 2004,
                LoadingScreenType = EGSLoadingScreenType.Unknown
            };

            BPS_EventCollectionCS.GetLocal(playerCharacter)?.Evt_BPS_TeleportTo.Invoke(
                ETeleportTypeV2.Dream,
                teleportParam,
                EPlayerTeleportReason.Dream
            );
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
                    if (tamer.IsTamerSynced)
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
            Utils.TryRunOnGameThread(TamerUtils.ClearEcsMonsters);
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
                var teamColor = PvPUtils.GetTeamColorString(playerState.TeamId);
                playerState.MarkerActor.CallFunctionByNameWithArguments($"SetText {playerState.NickName} {teamColor}", true);
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

        private void OnBeforeJoinedRoomCallback()
        {
            SetUpRoom();
            SpawnPlayersAlreadyInRoom();
            UpdateConnectedCount();
            if (!Constants.IsCoop)
            {
                var player = GameUtils.GetControlledPawn();
                SkillsUtils.DisableVigorSkill(player);
                SkillsUtils.DisableFaBaoSkill(player);
            }
#if TESTING
            BUC_SpeedCtrlData? speedCtrlData = BGU_DataUtil.GetUnPersistentReadOnlyData<IBUC_SpeedCtrlData, BUC_SpeedCtrlData>(GameUtils.GetControlledPawn()) as BUC_SpeedCtrlData;
            speedCtrlData?.SetSpeedInfo(10000, 10000, 10000);
#endif
            LobbyStatusWidget.Instance.SetReadyCount(Client.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
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
                Utils.TryRunOnGameThread(TamerUtils.DiscoverTamers);
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
            return SpawningUtils.AdjustSpawnLocation(Client.GetPlayerById(peerId)?.Pawn, baseLocation);
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
    }
}