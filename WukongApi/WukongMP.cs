using System;
using System.Linq;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using Photon.Realtime;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.Patches;
using WukongApi.State;
using WukongApi.UI;
using PlayerState = WukongApi.State.PlayerState;

namespace WukongApi
{
    using PlayerState = PlayerState;

    // ReSharper disable once InconsistentNaming
    public class WukongMP
    {
        public FreeCameraManager FreeCameraManager { get; } = new FreeCameraManager();

        public readonly Harmony Harmony = new Harmony("WukongMP");

        public WukongClient Photon { get; private set; }

        private FVector _savedPosition;

        private readonly ChatWidget _chatWidget = new ChatWidget();
        private readonly TimerWidget _timerWidget = new TimerWidget();
        private readonly LobbyStatusWidget _lobbyStatusWidget = new LobbyStatusWidget();
        private readonly GameMessageWidget _gameMessageWidget = new GameMessageWidget();
        private readonly InfoMessageWidget _infoMessageWidget = new InfoMessageWidget();
        private readonly CountdownWidget _countdownWidget = new CountdownWidget();

        public static WukongMP Instance { get; } = new WukongMP();

        public bool DisableArchiveSave { get; set; }

        private WukongMP()
        {
            // empty
        }

        public void Patch()
        {
            Utils.TryRunOnGameThread(() =>
            {
                Harmony.PatchCategory(Constants.GlobalPatches);
                Harmony.PatchCategory(Constants.ConnectedPatches);
                Logging.LogDebug("Patched with Harmony");
            });
        }

        public void Unpatch()
        {
            Utils.TryRunOnGameThread(() => { Harmony.UnpatchAll(); });
        }

        public void Init()
        {
            DisconnectIfConnected();
            InitPhotonAndConnectToChat();
            AsyncInitGameInstance();
        }

        private void AsyncInitGameInstance()
        {
            Logging.LogDebug("Waiting for the game instance to be initialized.");
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (GameUtils.IsGameInstanceValid())
                        {
                            Logging.LogDebug("Found valid GameInstance");
                            Utils.TryRunOnGameThread(() => InitWorldCallbacks());
                            break; // Exit the task
                        }

                        await Task.Delay(500);
                    }
                }
                catch (Exception e)
                {
                    Logging.LogError(e.ToString());
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
                BGW_EventCollection.Get(gameInstance).Evt_PostLoadingScreenClose += OnLoadingScreenClose;
            }
            else
            {
                Logging.LogError("GameInstance is not valid.");
            }
        }

        private void OnMapLoaded()
        {
            var world = GameUtils.GetWorld();
            if (world != null)
            {
                Logging.LogDebug($"New level loaded: {world.GetCurrentLevelName()}");
            }
        }

        private void OnDelayBeginPlay()
        {
            Logging.LogDebug("Delay begin play for player.");
            DestroyAllMonsters();
            if (!Photon.Ready)
            {
                BlueprintUIUtils.SpawnModActor();
                InitializeWidgets();
                Connect();
            }

            SetPlayerTransform(Constants.PvpStartingLocation, FRotator.ZeroRotator);
        }

        private void InitializeWidgets()
        {
            _chatWidget.Initialize();
            _chatWidget.SetVisibility(false);
            _timerWidget.Initialize();
            _lobbyStatusWidget.Initialize();
            _lobbyStatusWidget.SetMaxConnectedCount(Constants.MaxPlayers); // TODO: Set it from launcher value
            _gameMessageWidget.Initialize();
            _countdownWidget.Initialize();
            _infoMessageWidget.Initialize();
        }

        private void OnLoadingScreenClose()
        {
            _chatWidget.SetVisibility(true);
            SetupLobbyUI();
        }

        private void SetupLobbyUI()
        {
            _gameMessageWidget.SetVisibility(true);
            _gameMessageWidget.SetMainText(Texts.InMultiplayer);
            _gameMessageWidget.SetSecondText(Texts.PressToBeReady);
            _gameMessageWidget.SetThirdText(Texts.PressToSwitchTeam);
            _lobbyStatusWidget.SetVisibility(true);
        }

        public void DumpPlayerState()
        {
            // dump player state to console for me
            Logging.LogDebug($"Local player state: {Photon.LocalPlayerState}");
            // dump player state to console for each connected player
            foreach (var (id, state) in Photon.ConnectedPlayers)
            {
                Logging.LogDebug($"Player {id} state: {state}");
            }
        }

        public bool ShouldRunConnectedPatches()
        {
            return Photon != null && Photon.Ready && Photon.PhotonClient.InRoom;
        }

        private void StartPvP()
        {
            _timerWidget.SetVisibility(false);
            _gameMessageWidget.SetVisibility(false);
            Photon.StartPvP();
        }

        public void StartRound()
        {
            _timerWidget.StartRoundCountdown(0, 30, OnRoundEnded);
        }

        private void OnRoundEnded()
        {
            Logging.LogWarning($"Round time ended, ending round");
            if (Photon.IsMasterClient)
            {
                Task.Run(async () => await Photon.LobbyManager.EndRoundAsync(Constants.DrawTeamId));
            }
        }

        public void EndRound()
        {
            _timerWidget.StopCountdown();
        }
        public void EnablePvP()
        {
            Logging.LogDebug("Enabled PvP");

            var myTeam = Photon.LocalPlayerState.TeamId;
            var otherTeams = Photon.ConnectedPlayers.Values
                .Where(p => p.TeamId != myTeam)
                .Select(p => p.TeamId)
                .Distinct()
                .ToList();

            Logging.LogDebug($"My team: {myTeam}");
            Logging.LogDebug($"Other teams: {string.Join(", ", otherTeams)}");

            GameLoopPatch.QueueOnGameThread(() =>
            {
                foreach (var team in otherTeams)
                {
                    PhotonUtils.RegisterTeamHostility(myTeam, team);
                }
            }, "Register team hostility");
        }

        public void DisablePvP()
        {
            Logging.LogDebug("Disabled PvP");

            var myTeam = Photon.LocalPlayerState.TeamId;
            var otherTeams = Photon.ConnectedPlayers.Values
                .Where(p => p.TeamId != myTeam)
                .Select(p => p.TeamId)
                .Distinct()
                .ToList();

            Logging.LogDebug($"My team: {myTeam}");
            Logging.LogDebug($"Other teams: {string.Join(", ", otherTeams)}");

            GameLoopPatch.QueueOnGameThread(() =>
            {
                foreach (var team in otherTeams)
                {
                    PhotonUtils.UnregisterTeamHostility(myTeam, team);
                }
            }, "Register team hostility");
        }

        public void EndTurnament(int winnerTeamId)
        {
            Logging.LogDebug("End turnament");
            SetupLobbyUI();
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
                        Logging.LogDebug($"Spawning monster for tamer with guid: {guid}.");

                        if (!Photon.SyncedMonsters.ContainsKey(guid))
                        {
                            Photon.SyncedMonsters.Add(guid, new MonsterState(guid, actor));
                            Logging.LogDebug("Monster was not synced, adding to synced monsters.");
                        }

                        Logging.LogDebug("Invoking Evt_TamerBlockingSpawnImmediately.");
                        events.Evt_TamerBlockingSpawnImmediately.Invoke(guid);
                    }
                    else if (!Photon.SyncedMonsters.ContainsKey(guid))
                    {
                        Logging.LogDebug($"Monster already spawned but not synced: {guid}.");

                        var state = new MonsterState(guid, actor);
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

        private void InitPhotonAndConnectToChat()
        {
            Photon = new WukongClient(OnJoinedRoomCallback, p => { GameLoopPatch.QueueOnGameThread(() => SpawnCloneForPlayer(p), "SpawnCloneForPlayer"); });
            Photon.WukongChat.OnGetMessage += _chatWidget.GetMessage;
            Photon.WukongChat.OnReconnectRequest += Reconnect;
            Photon.WukongChat.OnDisconnectRequest += DisconnectIfConnected;
            Photon.WukongChat.OnRebirthRequested += () => { GameLoopPatch.QueueOnGameThread(() => Photon.BroadcastPlayerRebirth(Photon.LocalPlayerState.PhotonId), "HandleRebirth"); };
        }

        private void RebirthPlayer(int playerId)
        {
            Logging.LogDebug($"RebirthPlayer for player {playerId} called");

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

        public void DestroyAllMonsters()
        {
            AActor[] allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                BGU_UnrealWorldUtil.DestroyActor(actor);
            }
        }

        private void Connect()
        {
            if (Photon.Ready)
            {
                return;
            }

            Photon.OnBeforeJoinRoom += SetPlayerProperties;
            Photon.OnUnitSpawn += (_, guid, name, teamId, x, y, z) => GameLoopPatch.QueueOnGameThread(() => SpawnRemoteUnit(guid, name, teamId, x, y, z), "SpawnRemoteUnit");
            Photon.OnMontageCallback += (id, data) => GameLoopPatch.QueueOnGameThread(() => ApplyPlayerMontageCallback(id, data), "ApplyPlayerMontageCallback");
            Photon.OnMonsterMontageCallback += (id, data) => GameLoopPatch.QueueOnGameThread(() => ApplyMonsterMontageCallback(id, data), "ApplyMonsterMontageCallback");
            Photon.OnMonsterWakeUp += guid => GameLoopPatch.QueueOnGameThread(() => WakeUpMonster(guid), "WakeUpMonster");
            Photon.OnEquipmentChange += (id, eq) => GameLoopPatch.QueueOnGameThread(() => ChangeEquipment(id, eq), "ChangeEquipment");
            Photon.OnReadinessChange += (name, isReady, readyCount) => Utils.TryRunOnGameThread(() => UpdateReadiness(name, isReady, readyCount));
            Photon.OnTeamChange += (name, teamId) => Utils.TryRunOnGameThread(() => _lobbyStatusWidget.UpdatePlayerTeam(name, teamId));
            Photon.OnPlayerLeft += (pawn) => Utils.TryRunOnGameThread(() => RemovePlayer(pawn));
            Photon.OnDamageNum += damageNum => GameLoopPatch.QueueOnGameThread(() => OnDamageNum(damageNum), "OnDamageNum", BGW_TickGroupMask.TG_PreAnim);
            Photon.OnPlayerRebirth += id => GameLoopPatch.QueueOnGameThread(() => RebirthPlayer(id), "RebirthPlayer");
            Photon.OnKillPlayer += id => GameLoopPatch.QueueOnGameThread(() => KillPlayer(id), "KillPlayer");
            Photon.OnSetPlayerTransform += (loc, rot) => GameLoopPatch.QueueOnGameThread(() => SetPlayerTransform(loc, rot), "SetPlayerTransform");
            Photon.WukongChat.OnSendMessage += _chatWidget.AddMessage;
            Photon.WukongChat.OnSavePosition += SaveCurrentPosition;
            Photon.WukongChat.OnLoadPosition += LoadSavedPosition;
            Photon.WukongChat.OnSpawnEnemy += (name, count, teamId) => GameLoopPatch.QueueOnGameThread(() => SpawnEnemiesMaster(name, count, teamId), "SpawnEnemiesMaster");
            Photon.StartClient();
        }

        private void KillPlayer(int playerId)
        {
            var player = Photon.GetById(playerId).Pawn;
            if (player == null)
                return;

            var events = BUS_EventCollectionCS.Get(player);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            if (Photon.IsMasterClient)
            {
                events?.Evt_UnitDead.Invoke(player, EDeadReason.Suicide);
            }
        }

        private void SetPlayerTransform(FVector location, FRotator rotation)
        {
            GameUtils.GetBguPlayerCharacterCs()?.SetActorTransform(new FTransform(rotation, location), false, out _, true);
            GameUtils.GetPlayerController()?.SetControlRotation(rotation);
        }

        private void Reconnect()
        {
            DisconnectIfConnected();
            InitPhotonAndConnectToChat();
            Connect();
        }

        private void DisconnectIfConnected()
        {
            if (GameUtils.IsWorldValid())
            {
                UnsubscribeFromPlayerMontageCallbacks();
            }

            Photon?.StopClient();
            Photon = null;
        }

        private void SetPlayerProperties()
        {
            var player = GameUtils.GetControlledPawn();

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
        }

        private void ChangeEquipment(int id, EquipmentState eq)
        {
            if (!Photon.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Logging.LogDebug($"Player not found: {id}");
                return;
            }

            var clone = (BGUCharacterCS)player.Pawn;
            EquipmentHelpers.SetRemoteActorEquipment(clone, eq);
        }

        private void UpdateReadiness(string playerNickName, bool isReady, int readyCount)
        {
            if (Photon.IsMasterClient) // send this only once
            {
                Photon.WukongChat.SendChatMessage(WukongChatter.ServerChannelName, $"{playerNickName} is {(isReady ? "ready" : "not ready")}");
            }
            if (isReady)
            {
                if (readyCount == (Photon.ConnectedPlayers.Count + 1))
                {
                    // all players are ready
                    _countdownWidget.StartLobbyCountdown(5, StartPvP);
                    _gameMessageWidget.SetMainText(Texts.StartingGame);
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
                _gameMessageWidget.SetSecondText(Texts.PressToBeNotReady);
            }
            else
            {
                _gameMessageWidget.SetThirdText(Texts.PressToSwitchTeam);
                _gameMessageWidget.SetSecondText(Texts.PressToBeReady);
            }
        }

        private void RemovePlayer(APawn playerPawn)
        {
            BGU_UnrealWorldUtil.DestroyActor(playerPawn);
            UpdateConnectedCount();
        }

        private void UpdateConnectedCount()
        {
            _lobbyStatusWidget.SetConnectedCount(Photon.ConnectedPlayers.Count + 1);
        }

        private static void OnDamageNum(DamageNumParam damageNum)
        {
            var uiEvt = BGW_UIEventCollection.Get(GameUtils.GetWorld());
            uiEvt.Evt_UI_ShowHPChangeNum(damageNum);
        }

        private void ApplyPlayerMontageCallback(int id, MontageCallbackData data)
        {
            if (!Photon.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Logging.LogDebug($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(data.MontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage is null)
            {
                Logging.LogDebug($"Montage not found: {data.MontagePath}");
                return;
            }

            Logging.LogDebug($"Applying montage callback for player {id} with montage {data.MontagePath} ({data.Reason}, {data.State})");
            var animInstance = ((ACharacter)clone).Mesh.GetAnimInstance();

            if (animInstance is null)
            {
                Logging.LogDebug("AnimInstance is null");
                return;
            }

            if (data.State == EMontageCallbackState.OnStarted && animInstance.GetCurrentActiveMontage()?.PathName != montage.PathName)
            {
                animInstance.Montage_Play(montage);
            }
            else if (data.State == EMontageCallbackState.OnInterrupted)
            {
                if (animInstance.GetCurrentActiveMontage()?.PathName == montage.PathName)
                {
                    animInstance.Montage_Stop(1f, montage);
                }
            }

            var events = BUS_EventCollectionCS.Get(clone);
            events.Evt_PlayMontageCallback.Invoke(data.Reason, montage, data.State);
        }

        private void ApplyMonsterMontageCallback(int _, MonsterMontageCallbackData data)
        {
            if (!Photon.SyncedMonsters.TryGetValue(data.MonsterGuid, out var monster))
            {
                Logging.LogDebug($"Monster not found: {data.MonsterGuid}");
                return;
            }

            if (!monster.IsTamerValid)
                return;

            var tamerActor = monster.Pawn;

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(data.MontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage is null)
            {
                Logging.LogDebug($"Montage not found: {data.MontagePath}");
                return;
            }

            Logging.LogDebug($"Applying montage callback for monster {data.MonsterGuid} with montage {data.MontagePath} ({data.Reason}, {data.State})");
            if (tamerActor.GetMonster() == null)
            {
                Logging.LogError($"Monster is null in {nameof(ApplyMonsterMontageCallback)}");
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
                Logging.LogError($"events is null in {nameof(ApplyMonsterMontageCallback)}");
            }
        }

        private void SubscribeToPlayerMontageCallbacks()
        {
            var myPawn = GameUtils.GetControlledPawn();
            Photon.LocalPlayerState.Pawn = myPawn;

            var events = BUS_EventCollectionCS.Get(myPawn);
            events.Evt_PlayMontageCallback += OnPlayMontageCallback;
        }

        private void UnsubscribeFromPlayerMontageCallbacks()
        {
            var myPawn = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(myPawn);

            if (events != null)
            {
                events.Evt_PlayMontageCallback -= OnPlayMontageCallback;
            }
        }

        private void OnPlayMontageCallback(EMontageBindReason reason, UAnimMontage montage, EMontageCallbackState state)
        {
            var montagePath = montage.GetPathName();
            Logging.LogDebug($"Montage callback: {reason} {montagePath} {state}");
            Photon.SendMontageCallback(reason, montagePath, state);
        }

        private void SpawnEnemiesMaster(string enemyName, int count, int teamId)
        {
            var player = GameUtils.GetControlledPawn();
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

            Logging.LogDebug($"Sending spawn enemy {enemyName} at {loc}");
            Photon.SpawnUnit(id, unitName, teamId, loc.X, loc.Y, loc.Z);
        }

        private void SpawnRemoteUnit(string guid, string unitName, int teamId, float x, float y, float z)
        {
            SpawnUnitLocally(guid, unitName, teamId, x, y, z);
        }

        private void SpawnUnitLocally(string guid, string unitName, int teamId, float x, float y, float z)
        {
            Logging.LogDebug($"Spawn unit called for {unitName}");

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
                Logging.LogError("Could not spawn enemy: " + unitName);
                return;
            }

            buTamerActor.SpawnedTamerGuid = guid;
            // Update final guid
            buTamerActor.GetFinalGuid();

            UBGUFunctionLibrary.BGUFinishSpawningActor(buTamerActor, transform);
            Logging.LogDebug($"Spawned enemy: {buTamerActor.GetName()}, with guid {guid}");
            Photon.SyncedMonsters.Add(guid, new MonsterState(guid, buTamerActor, teamId));
        }

        private void LoadSavedPosition()
        {
            var pawn = GameUtils.GetControlledPawn();
            if (pawn != null)
            {
                pawn.SetActorLocation(_savedPosition, false, out _, true);
            }
        }

        private void SaveCurrentPosition()
        {
            var pawn = GameUtils.GetControlledPawn();
            if (pawn != null)
            {
                _savedPosition = pawn.GetActorLocation();
            }
        }

        private void OnJoinedRoomCallback()
        {
            SubscribeToPlayerMontageCallbacks();
            SpawnPlayersAlreadyInRoom();
            UpdateConnectedCount();
        }

        private void SpawnPlayersAlreadyInRoom()
        {
            // when joining game, spawn all players already in room
            foreach (var player in Photon.GetOtherPlayersInRoom())
            {
                GameLoopPatch.QueueOnGameThread(() => SpawnCloneForPlayer(player), "SpawnCloneForPlayer");
            }
        }

        public static APawn SpawnWukong(ABGPPlayerController oldController, UClass pawnClass, FTransform spawnTransform, APawn oldPawn)
        {
            var newPawn = BGU_UnrealActorUtil.BGUBeginDeferredActorSpawnFromClass(oldController.World, pawnClass, spawnTransform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as APawn;
            oldController.Possess(newPawn);
            var obj = newPawn as ACharacter;
            obj.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: false);
            obj.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: false);
            BGU_UnrealActorUtil.BGUFinishSpawningActorAndECSBeginPlay(oldController, newPawn, spawnTransform);
            BPS_GSEventCollection.Get(oldController).Evt_BPS_OnControlledPawnChange.Invoke(newPawn);
            BGS_EventCollectionCS.Get(oldController)?.Evt_NotifyPossessEntityChanged.Invoke(oldPawn.ToEntity(), newPawn.ToEntity());
            obj.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
            obj.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
            UGSE_ActorFuncLib.UpdateActorOverlaps(obj);
            return newPawn;
        }

        public static void BackToOldPawn(ABGPPlayerController oldController, APawn oldPawn, APawn newPawn)
        {
            oldController.UnPossess();
            oldController.Possess(oldPawn);
            BPS_GSEventCollection.Get(oldController).Evt_BPS_OnControlledPawnChange.Invoke(oldPawn);
            BGS_EventCollectionCS.Get(oldController)?.Evt_NotifyPossessEntityChanged.Invoke(newPawn.ToEntity(), oldPawn.ToEntity());
        }

        private void SpawnCloneForPlayer(Player player)
        {
            var id = player.ActorNumber;

            if (Photon.ConnectedPlayers.ContainsKey(id))
            {
                Logging.LogError($"Player already exists: {id}");
                return;
            }

            var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
            var oldPawn = GameUtils.GetControlledPawn();

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

            var @class = UClass.GetClass("BGUAIPlayerController"); // "BGPPlayerController" works for sure

            if (@class is null)
            {
                Logging.LogDebug("Class is null");
                return;
            }

            var oldController = GameUtils.GetPlayerController();
            var newPawn = SpawnWukong(oldController, playerPawnClass, new FTransform(rot, loc), oldPawn);

            BackToOldPawn(oldController, oldPawn, newPawn);

            Logging.LogDebug($"Assigned player {id} clone {newPawn.GetEntityHash()}");

            var newControllerActor = GameUtils.GetWorld().SpawnActor(@class, ref loc, ref rot);
            if (newControllerActor != null && newControllerActor is ABGUAIPlayerController newController)
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
            int teamId = Constants.AvailableTeamIds.First();
            if (player.CustomProperties.TryGetValue(nameof(PlayerState.TeamId), out var assignedTeamId))
            {
                teamId = (int)assignedTeamId;
            }

            var playerState = new PlayerState(id, newPawn, teamId)
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
                    Logging.LogDebug($"Setting remote player initial attribute {attr} = {value}");
                    playerState.Attributes[attr] = (float)value;
                }
            }

            // update equipment
            if (player.CustomProperties.TryGetValue(nameof(PlayerState.Equipment), out var eq))
            {
                playerState.Equipment = (EquipmentState)eq;
                EquipmentHelpers.SetRemoteActorEquipment((BGUCharacterCS)newPawn, playerState.Equipment);
            }

            // set lock distance
            var character = newPawn as BGUCharacterCS;
            FUStUnitCommDesc unitCommDesc = BGW_GameDB.GetUnitCommDesc(character.GetResID());
            if (unitCommDesc != null)
            {
                unitCommDesc.CameraLockDist = 10000;
            }

            Photon.RegisterPlayer(playerState);
            UpdateConnectedCount();
            _lobbyStatusWidget.UpdatePlayerTeam((string)nickName, teamId);
        }
    }
}