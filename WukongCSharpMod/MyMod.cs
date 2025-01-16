using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;

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

        private void InitUserName()
        {
            try
            {
                Helpers.Log($"Loading player name from {Path.Join(Directory.GetCurrentDirectory(), "PhotonUserName.txt")}");
                var allLines = File.ReadLines("PhotonUserName.txt").ToList();
                _userName = allLines[0];
                Helpers.Log($"Player name is = '{_userName}'");
            }
            catch (System.Exception ex)
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

            InitUserName();
            
            // InitWorldCallbacks();

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Helpers.Log("Alt + H");

                InitializeChatWidget();
                CleanUpPhoton();
                InitPhoton();
                // Connect();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            {
                Helpers.Log("Alt + V");

                SpawnAllMonsters();
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
        }

        // ReSharper disable once UnusedMember.Local
        private void InitWorldCallbacks()
        {
            UWorld world = GameUtils.GetWorld();
            if (world != null)
            {
                BGW_EventCollection.Get(world).Evt_PostLoadMapWithWorld += OnMapLoaded;
                BGW_EventCollection.Get(world).Evt_PlayerDelayBeginPlayFinished += OnDelayBeginPlay;
            }
            else
            {
                Helpers.Log("World is null.");
            }
        }

        private void PrintPlayerLocomotionData(AActor player)
        {
            var playerLocomotionData = BGU_DataUtil.GetUnPersistentReadOnlyData<IBUC_ABPPlayerLocomotionData, BUC_ABPPlayerLocomotionData>(player);
            Helpers.Log("PlayerLocomotionData:");
            var propertyInfos = typeof(BUC_ABPPlayerLocomotionData).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                var property = propertyInfo.GetValue(playerLocomotionData);
                Helpers.Log($"{propertyInfo.Name}: {property}");
            }
        }

        private static void SpawnAllMonsters()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                var events = BGS_GSEventCollection.Get(actor);
                if (events != null)
                {
                    if (actor.GetMonster() == null)
                    {
                        Helpers.Log($"Spawning monster for tamer with guid: {actor.CurrentRef.TamerGuid}.");
                        events.Evt_TamerBlockingSpawnImmediately.Invoke(actor.CurrentRef.TamerGuid);
                    }
                    else
                    {
                        Helpers.Log($"Monster already spawned for tamer with guid: {actor.CurrentRef.TamerGuid}.");
                    }
                }
                else
                {
                    Helpers.Log("Event is null");
                }
            }
        }

        private void CleanUpPhoton()
        {
            Photon?.StopClient();
        }

        private void InitPhoton()
        {
            Photon = new WukongClient(SpawnPlayersAlreadyInRoom, _userName);
            Photon.WukongChat.OnGetMessage += GetMessageFromWidget;
            Photon.WukongChat.OnConnectRequest += Connect;
        }

        private void OnMapLoaded()
        {
            UWorld world = GameUtils.GetWorld();
            if (world != null)
                Helpers.Log($"Map loaded: {world.GetCurrentLevelName()}");
        }

        private void OnDelayBeginPlay()
        {
            Helpers.Log("Delay begin play.");
            InitializeChatWidget();
            InitPhoton();
        }

        private void Connect()
        {
            if (Photon.Ready)
            {
                return;
            }

            Photon.OnPlayerJoined += id => Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id));
            Photon.OnUnitSpawn += (_, id, name, x, y, z) => Utils.TryRunOnGameThread(() => SpawnRemoteUnit(id, name, x, y, z));
            Photon.OnMontageCallback += (id, data) => Utils.TryRunOnGameThread(() => ApplyPlayerMontageCallback(id, data));
            Photon.OnMonsterMontageCallback += (id, data) => Utils.TryRunOnGameThread(() => ApplyMonsterMontageCallback(id, data));
            Photon.WukongChat.OnSendMessage += AddMessageToWidget;
            Photon.WukongChat.OnSavePosition += SaveCurrentPosition;
            Photon.WukongChat.OnLoadPosition += LoadSavedPosition;
            Photon.WukongChat.OnSpawnEnemy += (name, count) => Utils.TryRunOnGameThread(() => SpawnEnemiesMaster(name, count));

            Photon.StartClient();
            SubscribeToPlayerEvents();
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

        private void ApplyMonsterMontageCallback(int id, MonsterMontageCallbackData data)
        {
            if (!Photon.SyncedMonsters.TryGetValue(data.MonsterId, out var monster))
            {
                Helpers.Log($"Player not found: {id}");
                return;
            }

            var tamerActor = monster.Pawn;

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(data.MontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage is null)
            {
                Helpers.Log($"Montage not found: {data.MontagePath}");
                return;
            }

            Helpers.Log($"Applying montage callback for monster {data.MonsterId} with montage {data.MontagePath} ({data.Reason}, {data.State})");
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

        private void SpawnEnemiesMaster(string enemyName, int count)
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
                    Utils.TryRunOnGameThread(() => { SpawnEnemyMaster(enemyName, loc); });
                });
            }
        }

        private void SpawnEnemyMaster(string enemyName, FVector loc)
        {
            var unitName = UnitPathsConfig.GetUnitPath(enemyName);

            var id = Photon.SyncedMonsters.Count;
            SpawnUnitLocally(id, unitName, loc.X, loc.Y, loc.Z);

            Helpers.Log($"Sending spawn enemy {enemyName} at {loc}");
            Photon.SpawnUnit(id, unitName, loc.X, loc.Y, loc.Z);
        }

        private void SpawnRemoteUnit(int id, string unitName, float x, float y, float z)
        {
            SpawnUnitLocally(id, unitName, x, y, z);
        }

        private void SpawnUnitLocally(int id, string unitName, float x, float y, float z)
        {
            Helpers.Log($"Spawn unit called for {unitName}");

            if (string.IsNullOrEmpty(unitName))
                return;

            var loc = new FVector(x, y, z);
            var rot = new FRotator();

            var world = GameUtils.GetWorld();

            UClass cachedResourceObj = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitName, ELoadResourceType.SyncLoadAndCache);
            FTransform transform = new FTransform(rot, loc);
            BUTamerActor buTamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, (TSubclassOf<AActor>)cachedResourceObj, transform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null) as BUTamerActor;
            if (buTamerActor == null)
            {
                Helpers.LogError("Could not spawn enemy: " + unitName);
                return;
            }

            UBGUFunctionLibrary.BGUFinishSpawningActor(buTamerActor, transform);
            Helpers.Log("Spawned enemy: " + buTamerActor.GetName());

            Photon.SyncedMonsters.Add(id, new MonsterState(id, buTamerActor));
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

        private void SpawnPlayersAlreadyInRoom()
        {
            // when joining game, spawn all players already in room
            foreach (var id in Photon.GetOtherPlayersInRoom())
            {
                Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id));
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
            BGS_EventCollectionCS.Get(oldController)?.Evt_NotifyPossessEntityChanged.Invoke(ECSExtension.ToEntity(oldPawn), ECSExtension.ToEntity(newPawn));
            obj.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
            obj.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
            UGSE_ActorFuncLib.UpdateActorOverlaps(obj);
            return newPawn;
        }

        private void BackToOldPawn(ABGPPlayerController oldController, APawn oldPawn, APawn newPawn, FTransform spawnTransform)
        {
            oldController.UnPossess();
            oldController.Possess(oldPawn);
            ACharacter obj = oldPawn as ACharacter;
            BPS_GSEventCollection.Get(oldController).Evt_BPS_OnControlledPawnChange.Invoke(oldPawn);
            BGS_EventCollectionCS.Get(oldController)?.Evt_NotifyPossessEntityChanged.Invoke(ECSExtension.ToEntity(newPawn), ECSExtension.ToEntity(oldPawn));
        }

        private void SpawnCloneForJoiningPlayer(int id)
        {
            if (Photon.ConnectedPlayers.ContainsKey(id))
            {
                Helpers.Log($"Player already exists: {id}");
                return;
            }

            var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
            var oldPawn = GameUtils.GetControlledPawn();

            var loc = oldPawn.GetActorLocation() + new FVector(200, 200, 100);
            var rot = oldPawn.GetActorRotation();
            var @class = UClass.GetClass("BGUAIPlayerController"); // "BGPPlayerController" works for sure

            if (@class is null)
            {
                Helpers.Log("Class is null");
                return;
            }

            var oldController = GameUtils.GetPlayerController();
            var newPawn = SpawnWukong(oldController, playerPawnClass, new FTransform(rot, loc), oldPawn);

            BackToOldPawn(oldController, oldPawn, newPawn, oldPawn.GetActorTransform());
            // assign in dictionary
            Photon.ConnectedPlayers[id] = new PlayerState(id, newPawn);
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