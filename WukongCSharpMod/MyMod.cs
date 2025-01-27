using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using BtlB1;
using CommB1;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using Photon.Realtime;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongCSharpMod.State;
using PlayerState = WukongCSharpMod.State.PlayerState;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        private UUserWidget _chatWidget;

        public WukongClient Photon { get; private set; }

        public readonly Harmony Harmony = new Harmony("WukongMP");

        private FVector _savedPosition;
        private string _userName;

        public static MyMod Instance { get; private set; }

        private bool _multiplayerEnabled;

        private void InitUserName()
        {
            try
            {
                Helpers.Log($"Loading player name from {Path.Join(Directory.GetCurrentDirectory(), "PhotonUserName.txt")}");
                var allLines = File.ReadLines("PhotonUserName.txt").ToList();
                _userName = allLines[0];
                Helpers.Log($"Player name is = '{_userName}'");
            }
            catch (Exception ex)
            {
                Helpers.LogError("Couldn't player name from file");
                Helpers.LogError(ex.ToString());
                _userName = Constants.DefaultPhotonUserName;
            }
        }

        public void Init()
        {
            Instance = this;

            Helpers.Log("Init");

            Harmony.PatchAllUncategorized();

            InitUserName();

            // InitWorldCallbacks();

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Helpers.Log("Alt + H");

                InitializeChatWidget();
                CleanUpPhoton();
                InitPhoton();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            {
                Helpers.Log("Alt + V");

                var myTeam = Photon.LocalPlayerState.TeamId;
                var otherTeams = Photon.ConnectedPlayers.Values
                    .Where(p => p.TeamId != myTeam)
                    .Select(p => p.TeamId)
                    .Distinct()
                    .ToList();

                Helpers.Log($"My team: {myTeam}");
                Helpers.Log($"Other teams: {string.Join(", ", otherTeams)}");

                Utils.TryRunOnGameThread(() =>
                {
                    foreach (var team in otherTeams)
                    {
                        PhotonUtils.RegisterTeamHostility(myTeam, team);
                    }
                });
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                Helpers.Log("Alt + C");

                // dump player state to console for me
                Helpers.Log($"Local player state: {Photon.LocalPlayerState}");
                // dump player state to console for each connected player
                foreach (var (id, state) in Photon.ConnectedPlayers)
                {
                    Helpers.Log($"Player {id} state: {state}");
                }
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.A, () =>
            {
                Helpers.Log("Alt + A");
                Photon.SpawnClone();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.M, () =>
            {
                Helpers.Log("Alt + M");
                if (_multiplayerEnabled)
                    return;

                var world = GameUtils.GetWorld();
                if (world == null)
                    return;
                ArchiveSummaryData latestArchive = BGW_GameArchiveMgr.Get(world).GetLatestArchive();
                if (latestArchive == null)
                    return;

                Utils.TryRunOnGameThread(() =>
                {
                    Harmony.PatchCategory(Assembly.GetExecutingAssembly(), Constants.MultiplayerPatches);
                    Helpers.Log("Multiplayer mode patched with Harmony");
                });

                // Load archive
                BGW_EventCollection.Get(world).Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.LoadArchive, (object)new FSMInputData_GI_Global_SubG_GI_Loading_TravelLevel()
                {
                    ArchiveId = latestArchive.ArchiveId
                });
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
                        Helpers.Log($"Spawning monster for tamer with guid: {guid}.");

                        if (!Photon.SyncedMonsters.ContainsKey(guid))
                        {
                            Photon.SyncedMonsters.Add(guid, new MonsterState(guid, actor));
                            Helpers.Log("Monster was not synced, adding to synced monsters.");
                        }

                        Helpers.Log("Invoking Evt_TamerBlockingSpawnImmediately.");
                        events.Evt_TamerBlockingSpawnImmediately.Invoke(guid);
                    }
                    else if (!Photon.SyncedMonsters.ContainsKey(guid))
                    {
                        Helpers.Log($"Monster already spawned but not synced: {guid}.");

                        var state = new MonsterState(guid, actor);
                        Photon.SyncedMonsters.Add(guid, state);

                        PhotonUtils.PrepareMonsterForSync(Photon, state);
                    }
                }
                else
                {
                    Helpers.Log("Event is null");
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
            Photon = new WukongClient(_userName, OnJoinedRoomCallback, p => { Utils.TryRunOnGameThread(() => SpawnCloneForPlayer(p)); });
            Photon.WukongChat.OnGetMessage += GetMessageFromWidget;
            Photon.WukongChat.OnConnectRequest += Connect;
        }

        private void Connect()
        {
            if (Photon.Ready)
            {
                return;
            }

            Photon.OnBeforeJoinRoom += SetPlayerProperties;
            Photon.OnUnitSpawn += (_, guid, name, teamId, x, y, z) => Utils.TryRunOnGameThread(() => SpawnRemoteUnit(guid, name, teamId, x, y, z));
            Photon.OnMontageCallback += (id, data) => Utils.TryRunOnGameThread(() => ApplyPlayerMontageCallback(id, data));
            Photon.OnMonsterMontageCallback += (id, data) => Utils.TryRunOnGameThread(() => ApplyMonsterMontageCallback(id, data));
            Photon.OnMonsterWakeUp += guid => Utils.TryRunOnGameThread(() => WakeUpMonster(guid));
            Photon.OnEquipmentChange += (id, position, newEq) => Utils.TryRunOnGameThread(() => ChangeEquipment(id, position, newEq));
            Photon.WukongChat.OnSendMessage += AddMessageToWidget;
            Photon.WukongChat.OnSavePosition += SaveCurrentPosition;
            Photon.WukongChat.OnLoadPosition += LoadSavedPosition;
            Photon.WukongChat.OnSpawnEnemy += (name, count, teamId) => Utils.TryRunOnGameThread(() => SpawnEnemiesMaster(name, count, teamId));

            Photon.StartClient();
        }

        private void SetPlayerProperties()
        {
            var player = GameUtils.GetControlledPawn();

            Photon.CachePlayerProperty(nameof(PlayerState.Location), player.GetActorLocation());
            Photon.CachePlayerProperty(nameof(PlayerState.Rotation), player.GetActorRotation());

            // equipment
            var roleData = BGU_DataUtil.GetReadOnlyData<IBPC_RoleBaseData, BPC_RoleBaseData>(player.PlayerState);

            foreach (var (position, id) in roleData.EquipList)
            {
                Photon.CachePlayerProperty($"{Constants.EquipmentPrefix}{position}", id);
            }

            Photon.SetCachedPlayerProperties();
        }

        private void ChangeEquipment(int id, EquipPosition position, int newEq)
        {
            Helpers.Log($"Change equipment for player {id} at position {position} to {newEq}");

            if (!Photon.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Helpers.Log($"Player not found: {id}");
                return;
            }

            var clone = (BGUCharacterCS)player.Pawn;

            var equipComp = Traverse.Create(clone.ActorCompContainerCS).Field<List<UActorCompBaseCS>>("CompCSs").Value
                .FirstOrDefault(x => x is BUS_EquipComp);

            if (equipComp == null)
            {
                Helpers.LogError("EquipComp is null");
                return;
            }

            var eq = (BUS_EquipComp)equipComp;
            Traverse.Create(eq).Method("OnChangeEquipReal", position, newEq).GetValue();
        }

        private void ApplyPlayerMontageCallback(int id, MontageCallbackData data)
        {
            if (!Photon.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Helpers.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(data.MontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage is null)
            {
                Helpers.Log($"Montage not found: {data.MontagePath}");
                return;
            }

            Helpers.Log($"Applying montage callback for player {id} with montage {data.MontagePath} ({data.Reason}, {data.State})");
            var animInstance = ((ACharacter)clone).Mesh.GetAnimInstance();

            if (animInstance is null)
            {
                Helpers.Log("AnimInstance is null");
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
                Helpers.Log($"Monster not found: {data.MonsterGuid}");
                return;
            }

            if (!monster.IsTamerValid)
                return;

            var tamerActor = monster.Pawn;

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(data.MontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage is null)
            {
                Helpers.Log($"Montage not found: {data.MontagePath}");
                return;
            }

            Helpers.Log($"Applying montage callback for monster {data.MonsterGuid} with montage {data.MontagePath} ({data.Reason}, {data.State})");
            if (tamerActor.GetMonster() == null)
            {
                Helpers.LogError($"Monster is null in {nameof(ApplyMonsterMontageCallback)}");
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
                Helpers.LogError($"events is null in {nameof(ApplyMonsterMontageCallback)}");
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
            Helpers.Log($"Montage callback: {reason} {montagePath} {state}");
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
                Helpers.Log("Spawning enemy by line trace");
            }
            else
            {
                centerLoc = player.GetActorLocation() + player.GetActorForwardVector() * Constants.MonsterSpawnDistance;
                Helpers.Log("Spawning enemy by player forward vector");
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
                    Utils.TryRunOnGameThread(() => { SpawnEnemyMaster(enemyName, loc, teamID); });
                });
            }
        }

        private void SpawnEnemyMaster(string enemyName, FVector loc, int teamId)
        {
            var unitName = UnitPathsConfig.GetUnitPath(enemyName);

            var id = Guid.NewGuid().ToString(); // TODO: use ActorGuid
            SpawnUnitLocally(id, unitName, teamId, loc.X, loc.Y, loc.Z);

            Helpers.Log($"Sending spawn enemy {enemyName} at {loc}");
            Photon.SpawnUnit(id, unitName, teamId, loc.X, loc.Y, loc.Z);
        }

        private void SpawnRemoteUnit(string guid, string unitName, int teamID, float x, float y, float z)
        {
            SpawnUnitLocally(guid, unitName, teamID, x, y, z);
        }

        private void SpawnUnitLocally(string guid, string unitName, int teamID, float x, float y, float z)
        {
            Helpers.Log($"Spawn unit called for {unitName}");

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
                Helpers.LogError("Could not spawn enemy: " + unitName);
                return;
            }

            buTamerActor.SpawnedTamerGuid = guid;
            // Update final guid
            buTamerActor.GetFinalGuid();

            UBGUFunctionLibrary.BGUFinishSpawningActor(buTamerActor, transform);
            Helpers.Log($"Spawned enemy: {buTamerActor.GetName()}, with guid {guid}");
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
                Utils.TryRunOnGameThread(() => SpawnCloneForPlayer(player));
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
                Helpers.LogError($"Player already exists: {id}");
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

            Helpers.Log($"Player {id} location: {loc}");
            Helpers.Log($"Player {id} rotation: {rot}");

            var @class = UClass.GetClass("BGUAIPlayerController"); // "BGPPlayerController" works for sure

            if (@class is null)
            {
                Helpers.Log("Class is null");
                return;
            }

            var oldController = GameUtils.GetPlayerController();
            var newPawn = SpawnWukong(oldController, playerPawnClass, new FTransform(rot, loc), oldPawn);

            BackToOldPawn(oldController, oldPawn, newPawn);

            Helpers.Log($"Assigned player {id} clone {newPawn.GetEntityHash()}");

            var newControllerActor = GameUtils.GetWorld().SpawnActor(@class, ref loc, ref rot);
            if (newControllerActor != null && newControllerActor is ABGUAIPlayerController newController)
            {
                Helpers.Log("Spawned new controller");
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

            // force update equipment
            var equipComp = Traverse.Create(((BGUCharacterCS)newPawn).ActorCompContainerCS).Field<List<UActorCompBaseCS>>("CompCSs").Value
                .FirstOrDefault(x => x is BUS_EquipComp);

            var onChangeEq = typeof(BUS_EquipComp).GetMethod("OnChangeEquipReal", BindingFlags.NonPublic | BindingFlags.Instance);

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipAccessory), out var eqAccessory))
            {
                playerState.EquipAccessory = (int)eqAccessory;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Accessory, playerState.EquipAccessory });
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipArm), out var eqArm))
            {
                playerState.EquipArm = (int)eqArm;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Arm, playerState.EquipArm });
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipFabao), out var eqFabao))
            {
                playerState.EquipFabao = (int)eqFabao;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Fabao, playerState.EquipFabao });
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipFoot), out var eqFoot))
            {
                playerState.EquipFoot = (int)eqFoot;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Foot, playerState.EquipFoot });
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipHead), out var eqHead))
            {
                playerState.EquipHead = (int)eqHead;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Head, playerState.EquipHead });
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipHulu), out var eqHulu))
            {
                playerState.EquipHulu = (int)eqHulu;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Hulu, playerState.EquipHulu });
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipUpwear), out var eqUpwear))
            {
                playerState.EquipUpwear = (int)eqUpwear;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Upwear, playerState.EquipUpwear });
            }

            if (player.CustomProperties.TryGetValue(nameof(PlayerState.EquipWeapon), out var eqWeapon))
            {
                playerState.EquipWeapon = (int)eqWeapon;
                onChangeEq.Invoke(equipComp, new object[] { EquipPosition.Weapon, playerState.EquipWeapon });
            }

            Photon.RegisterPlayer(playerState);
        }

        private void AddMessageToWidget(bool isServerMesssage, string sender, string message)
        {
            if (_chatWidget != null)
            {
                Helpers.Log($"Calling AddMessage function with message {message} from {sender}");
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
                    Helpers.Log($"Got message: {message} in GetSentMessage function");
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
                    Helpers.Log("Chat widget initialized!.");
                }
            }
        }

        public void DeInit()
        {
            Helpers.Log("DeInit");
        }
    }
}