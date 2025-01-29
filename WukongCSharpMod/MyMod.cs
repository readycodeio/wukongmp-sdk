using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using CommB1;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using Photon.Realtime;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongCSharpMod.Patches;
using WukongCSharpMod.State;
using Log = Photon.Realtime.Log;
using PlayerState = WukongCSharpMod.State.PlayerState;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        private UUserWidget _chatWidget;

        private WukongClient _photon;

        public WukongClient Photon
        {
            get
            {
                if (_photon == null)
                {
                    Logging.LogError("Photon is null");
                    // log stack trace
                    Logging.LogError(Environment.StackTrace);
                }

                return _photon;
            }
            private set => _photon = value;
        }

        public readonly Harmony Harmony = new Harmony("WukongMP");

        private FVector _savedPosition;
        private string _userName;

        public static MyMod Instance { get; private set; }

        private bool _multiplayerEnabled;

        private void InitUserName()
        {
            try
            {
                Logging.LogDebug($"Loading player name from {Path.Join(Directory.GetCurrentDirectory(), "PhotonUserName.txt")}");
                var allLines = File.ReadLines("PhotonUserName.txt").ToList();
                _userName = allLines[0];
                Logging.LogDebug($"Player name is = '{_userName}'");
            }
            catch (Exception ex)
            {
                Logging.LogError("Couldn't player name from file");
                Logging.LogError(ex.ToString());
                _userName = Constants.DefaultPhotonUserName;
            }
        }

        public void Init()
        {
            Instance = this;

            Logging.LogDebug("Init");

            Harmony.PatchAllUncategorized();

            InitUserName();

            // InitWorldCallbacks();

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Logging.LogDebug("Alt + H");

                InitializeChatWidget();
                CleanUpPhoton();
                InitPhoton();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                Logging.LogDebug("Alt + C");

                // dump player state to console for me
                Logging.LogDebug($"Local player state: {Photon.LocalPlayerState}");
                // dump player state to console for each connected player
                foreach (var (id, state) in Photon.ConnectedPlayers)
                {
                    Logging.LogDebug($"Player {id} state: {state}");
                }
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            {
                Logging.LogDebug("Alt + V");
                Photon.SpawnClone();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.M, () =>
            {
                Logging.LogDebug("Alt + M");
                if (_multiplayerEnabled)
                    return;

                var world = GameUtils.GetWorld();
                if (world == null)
                    return;
                ArchiveSummaryData latestArchive = BGW_GameArchiveMgr.Get(world).GetLatestArchive();
                if (latestArchive == null)
                    return;

                GameLoopPatch.QueueOnGameThread(() =>
                {
                    Harmony.PatchCategory(Assembly.GetExecutingAssembly(), Constants.MultiplayerPatches);
                    Logging.LogDebug("Multiplayer mode patched with Harmony");
                });

                // Load archive
                BGW_EventCollection.Get(world).Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.LoadArchive, new FSMInputData_GI_Global_SubG_GI_Loading_TravelLevel
                {
                    ArchiveId = latestArchive.ArchiveId
                });
            });
        }

        private void EnablePvP()
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
            });
        }

        public void SetMultiplayerEnabled()
        {
            _multiplayerEnabled = true;
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

        private void CleanUpPhoton()
        {
            UnsubscribeFromPlayerEvents();
            Photon?.StopClient();
        }

        private void InitPhoton()
        {
            Photon = new WukongClient(_userName, OnJoinedRoomCallback, p => { GameLoopPatch.QueueOnGameThread(() => SpawnCloneForPlayer(p)); });
            Photon.WukongChat.OnGetMessage += GetMessageFromWidget;
            Photon.WukongChat.OnConnectRequest += Connect;
            Photon.WukongChat.OnEnablePvP += EnablePvP;
            Photon.WukongChat.OnRebirthRequested += HandleRebirth;
        }

        private void HandleRebirth()
        {
            APawn curPlayer = Photon.LocalPlayerState.Pawn;
            IBUC_UnitStateData readOnlyData = BGU_DataUtil.GetReadOnlyData<IBUC_UnitStateData, BUC_UnitStateData>(curPlayer);
            if (readOnlyData == null)
            {
                return;
            }

            if (!readOnlyData.HasState(EBGUUnitState.Dead))
            {
                return;
            }

            BUS_EventCollectionCS.Get(curPlayer)?.Evt_UnitRebirth.Invoke(ERebirthType.Quick);
            BUS_EventCollectionCS.Get(curPlayer)?.Evt_TriggerPlayerRest.Invoke();
        }

        private void Connect()
        {
            if (Photon.Ready)
            {
                return;
            }

            Photon.OnBeforeJoinRoom += SetPlayerProperties;
            Photon.OnUnitSpawn += (_, guid, name, teamId, x, y, z) => GameLoopPatch.QueueOnGameThread(() => SpawnRemoteUnit(guid, name, teamId, x, y, z));
            Photon.OnMontageCallback += (id, data) => GameLoopPatch.QueueOnGameThread(() => ApplyPlayerMontageCallback(id, data));
            Photon.OnMonsterMontageCallback += (id, data) => GameLoopPatch.QueueOnGameThread(() => ApplyMonsterMontageCallback(id, data));
            Photon.OnMonsterWakeUp += guid => GameLoopPatch.QueueOnGameThread(() => WakeUpMonster(guid));
            Photon.OnEquipmentChange += (id, eq) => GameLoopPatch.QueueOnGameThread(() => ChangeEquipment(id, eq));
            Photon.OnDamageNum += (damageNum) => GameLoopPatch.QueueOnAnimThread(() => OnDamageNum(damageNum));
            Photon.WukongChat.OnSendMessage += AddMessageToWidget;
            Photon.WukongChat.OnSavePosition += SaveCurrentPosition;
            Photon.WukongChat.OnLoadPosition += LoadSavedPosition;
            Photon.WukongChat.OnSpawnEnemy += (name, count, teamId) => GameLoopPatch.QueueOnGameThread(() => SpawnEnemiesMaster(name, count, teamId));

            Photon.StartClient();
        }

        private void SetPlayerProperties()
        {
            var player = GameUtils.GetControlledPawn();

            Photon.CachePlayerProperty(nameof(PlayerState.Location), player.GetActorLocation());
            Photon.CachePlayerProperty(nameof(PlayerState.Rotation), player.GetActorRotation());

            // equipment
            var eq = EquipmentHelpers.GetCurrentEquipmentStateForActor(player);
            Photon.CachePlayerProperty(nameof(PlayerState.Equipment), eq);

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

        private void SubscribeToPlayerEvents()
        {
            var myPawn = GameUtils.GetControlledPawn();
            Photon.LocalPlayerState.Pawn = myPawn;

            var events = BUS_EventCollectionCS.Get(myPawn);
            events.Evt_PlayMontageCallback += OnPlayMontageCallback;
        }

        private void UnsubscribeFromPlayerEvents()
        {
            var myPawn = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(myPawn);
            events.Evt_PlayMontageCallback -= OnPlayMontageCallback;
        }

        private void OnPlayMontageCallback(EMontageBindReason reason, UAnimMontage montage, EMontageCallbackState state)
        {
            var montagePath = montage.GetPathName();
            Logging.LogDebug($"Montage callback: {reason} {montagePath} {state}");
            Photon.SendMontageCallback(reason, montagePath, state);
        }

        private void SpawnEnemiesMaster(string enemyName, int count, int teamID)
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
                    GameLoopPatch.QueueOnGameThread(() => { SpawnEnemyMaster(enemyName, loc, teamID); });
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

        private void SpawnRemoteUnit(string guid, string unitName, int teamID, float x, float y, float z)
        {
            SpawnUnitLocally(guid, unitName, teamID, x, y, z);
        }

        private void SpawnUnitLocally(string guid, string unitName, int teamID, float x, float y, float z)
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
            Photon.SyncedMonsters.Add(guid, new MonsterState(guid, buTamerActor, teamID));
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
            SubscribeToPlayerEvents();
            SpawnPlayersAlreadyInRoom();
        }

        private void SpawnPlayersAlreadyInRoom()
        {
            // when joining game, spawn all players already in room
            foreach (var player in Photon.GetOtherPlayersInRoom())
            {
                GameLoopPatch.QueueOnGameThread(() => SpawnCloneForPlayer(player));
            }
        }

        private APawn SpawnWukong(ABGPPlayerController oldController, UClass pawnClass, FTransform spawnTransform, APawn oldPawn)
        {
            APawn newPawn = BGU_UnrealActorUtil.BGUBeginDeferredActorSpawnFromClass(oldController.World, pawnClass, spawnTransform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as APawn;
            oldController.Possess(newPawn);
            ACharacter obj = newPawn as ACharacter;
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

        private static void BackToOldPawn(ABGPPlayerController oldController, APawn oldPawn, APawn newPawn)
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

            Logging.LogDebug($"Player {id} location: {loc}");
            Logging.LogDebug($"Player {id} rotation: {rot}");

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

            // assign in dictionary
            var teamId = PhotonUtils.GetTeamIdForPlayer(id);

            var playerState = new PlayerState(id, newPawn, teamId)
            {
                Location = loc,
                Rotation = rot
            };

            // update equipment
            if (player.CustomProperties.TryGetValue(nameof(PlayerState.Equipment), out var eq))
            {
                playerState.Equipment = (EquipmentState)eq;
                EquipmentHelpers.SetRemoteActorEquipment((BGUCharacterCS)newPawn, playerState.Equipment);
            }

            Photon.RegisterPlayer(playerState);
        }

        private void AddMessageToWidget(bool isServerMesssage, string sender, string message)
        {
            if (_chatWidget != null)
            {
                Logging.LogDebug($"Calling AddMessage function with message {message} from {sender}");
                _chatWidget.CallFunctionByNameWithArguments($"AddMessage {isServerMesssage} {sender} {message}", true);
            }
            else
            {
                InitializeChatWidget();
            }
        }

        private string GetMessageFromWidget()
        {
            if (_chatWidget != null)
            {
                _chatWidget.CallFunctionByNameWithArguments("GetSentMessage", true);
                var message = _chatWidget.ToolTipText.ToString();
                if (message.Length > 0)
                {
                    Logging.LogDebug($"Got message: {message} in GetSentMessage function");
                }

                return message;
            }

            InitializeChatWidget();
            return "";
        }

        private void InitializeChatWidget()
        {
            var widgets = GameUtils.GetWidgets();
            if (widgets != null)
            {
                if (widgets.Count == 1)
                {
                    _chatWidget = widgets[0];
                    Logging.LogDebug("Chat widget initialized!.");
                }
            }
        }

        public void DeInit()
        {
            Logging.LogDebug("DeInit");
        }
    }
}