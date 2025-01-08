using System.Collections.Generic;
using System.Reflection;
using b1;
using b1.BGW;
using BtlShare;
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

        private readonly Harmony _harmony = new Harmony("WukongMP");

        private readonly Dictionary<byte, MonsterState> _monsters = new Dictionary<byte, MonsterState>();

        private FVector _savedPosition;

        public static MyMod Instance { get; private set; }

        public void Init()
        {
            Instance = this;

            Helpers.Log("Init");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Helpers.Log("Patched with Harmony");

            // InitWorldCallbacks();

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Helpers.Log("Alt + X");

                InitPhoton();
                Connect();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Helpers.Log("Alt + H");

                InitializeChatWidget();
                InitPhoton();
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

        private void InitPhoton()
        {
            var myLocation = GameUtils.GetControlledPawn().GetActorTransform().GetLocation();
            Photon = new WukongClient(SpawnPlayersAlreadyInRoom, myLocation.X, myLocation.Y, myLocation.Z);
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
            Photon.OnKeyReceived += (id, key) => Utils.TryRunOnGameThread(() => ApplyPlayerInput(id, key));
            Photon.OnUnitSpawn += (_, id, name, x, y, z) => Utils.TryRunOnGameThread(() => SpawnRemoteUnit(id, name, x, y, z));
            Photon.OnRollSkill += (id, dir) => Utils.TryRunOnGameThread(() => ApplyRollSkill(id, (ESkillDirection)dir));
            Photon.WukongChat.OnSendMessage += AddMessageToWidget;
            Photon.WukongChat.OnSavePosition += SaveCurrentPosition;
            Photon.WukongChat.OnLoadPosition += LoadSavedPosition;
            Photon.WukongChat.OnSpawnEnemy += name => Utils.TryRunOnGameThread(() => SpawnEnemy(name));

            Photon.StartClient();
            SubscribeToPlayerEvents();
        }

        private void SubscribeToPlayerEvents()
        {
            var myPawn = GameUtils.GetControlledPawn();
            Photon.LocalPlayerState.Pawn = myPawn;

            // var events = BUS_EventCollectionCS.Get(myPawn);
        }

        private void UnsubscribeFromPlayerEvents()
        {
            // var myPawn = GameUtils.GetControlledPawn();
            // var events = BUS_EventCollectionCS.Get(myPawn);
        }

        private void SpawnEnemy(string enemyName)
        {
            var unitName = UnitPathsConfig.GetUnitPath(enemyName);

            var loc = Global.CameraLookPosition;

            var id = (byte)_monsters.Count; // TODO: Overflow??
            var pawn = SpawnUnit(id, unitName, loc.X, loc.Y, loc.Z, false);

            _monsters[id] = new MonsterState
            {
                Id = id,
                Local = true,
                Pawn = pawn
            };

            Helpers.Log($"Sending spawn enemy {enemyName} at {loc}");
            Photon.SpawnUnit(id, unitName, loc.X, loc.Y, loc.Z);
        }

        private void SpawnRemoteUnit(byte id, string unitName, float x, float y, float z)
        {
            SpawnUnit(id, unitName, x, y, z, true);
        }

        private BUTamerActor SpawnUnit(byte id, string unitName, float x, float y, float z, bool remote)
        {
            Helpers.Log($"Spawn unit called for {unitName}");

            if (string.IsNullOrEmpty(unitName))
                return null;

            var loc = new FVector(x, y, z);
            var rot = new FRotator();

            var world = GameUtils.GetWorld();

            UClass cachedResourceObj = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitName, ELoadResourceType.SyncLoadAndCache);
            AActor actor = BGUFunctionLibraryCS.BGUSpawnActor(world, (TSubclassOf<AActor>)cachedResourceObj, loc, rot);
            BUTamerActor buTamerActor = actor as BUTamerActor;
            FTamerRef currentRef = buTamerActor.CurrentRef;
            FieldInfo field = typeof(FTamerRef).GetField("_phase", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field.GetValue(currentRef).ToString() == "Dead")
                field.SetValue(currentRef, ETamerPhase.PreBegunPlay);
            currentRef.OverrideResetType = EBGUResetType.None;
            currentRef.GroupOverrideResetType = EBGUResetType.None;
            buTamerActor.TamerType = ETamerType.Spawned;
            Helpers.Log("Spawned enemy: " + buTamerActor.GetName());

            if (!remote)
                return buTamerActor;

            _monsters.Add(id, new MonsterState
            {
                Id = id,
                Local = false,
                Pawn = buTamerActor
            });

            var events = BUS_EventCollectionCS.Get(buTamerActor);

            if (events is null)
            {
                Helpers.Log("Events is null");
                return buTamerActor;
            }

            events.Evt_AIPerceptionSetting.Invoke(false);
            events.Evt_AIPauseBT.Invoke(true);

            return buTamerActor;
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

        private void SpawnCloneForJoiningPlayer(int id)
        {
            if (Photon.ConnectedPlayers.ContainsKey(id))
            {
                Helpers.Log($"Player already exists: {id}");
                return;
            }

            UnsubscribeFromPlayerEvents();

            var controller = GameUtils.GetPlayerController();
            var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
            var oldPawn = GameUtils.GetControlledPawn();
            var oldPawnPos = oldPawn.GetActorTransform().GetLocation();
            var oldPawnRot = oldPawn.GetActorTransform().GetRotation();

            BGUFuncLibPlayer.SpwanAndPossesPlayerContrlledPawn(controller, playerPawnClass, oldPawn.GetActorTransform(), pawn => { }, new BGUFuncLibPlayer.SpawnControlledPawnBlendParam
            {
                NeedBlend = false
            });

            // BGU_UnrealWorldUtil.DestroyActor(oldPawn);
            var clone = oldPawn;

            var cloneCharacter = clone as BGUPlayerCharacterCS;

            FActorSpawnParameters spawnInfo = new FActorSpawnParameters
            {
                Instigator = cloneCharacter.GetInstigator(),
                SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod.AlwaysSpawn,
                OverrideLevel = cloneCharacter.GetLevel(),
                ObjectFlags = EObjectFlags.Transient // We never want to save AI controllers into a map
            };

            var loc = cloneCharacter.GetActorLocation();
            var rot = cloneCharacter.GetActorRotation();

            // var @class = UClass.GetClass("BGPPlayerController"); // "BGPPlayerController" works for sure
            var @class = UClass.GetClass("BGUAIPlayerController"); // "BGPPlayerController" works for sure

            if (@class is null)
            {
                Helpers.Log("Class is null");
                return;
            }

            var newController = GameUtils.GetWorld().SpawnActor(@class, ref loc, ref rot, ref spawnInfo);

            Helpers.Log("Spawned new controller");

            if (newController != null && newController is ABGUAIPlayerController ctrl)
            {
                ctrl.Possess(clone);
                ctrl.CanBeDamaged = false;
                Helpers.Log("Possessed new controller");
            }

            // assign in dictionary
            Photon.ConnectedPlayers[id] = new PlayerState(id, clone);
            Helpers.Log($"Assigned player {id} clone {clone.GetEntityHash()}");

            var controlledPawn = GameUtils.GetControlledPawn();
            controlledPawn.SetActorTransform(new FTransform(oldPawnRot, oldPawnPos), false, out _, false);

            SubscribeToPlayerEvents();
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

        private void ApplyPlayerInput(int id, KeyPress keyPress)
        {
            if (!Photon.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Helpers.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;

            var events = BUS_EventCollectionCS.Get(clone);

            Helpers.Log($"Player {id} pressed key {keyPress.Key} with state {keyPress.State}");

            switch (keyPress.Key)
            {
                case PlayerInput.LightAttack:
                {
                    events.Evt_InputCastSkill.Invoke(EInputActionType.LightAttack, keyPress.State == KeyState.Released);
                    break;
                }
                case PlayerInput.HeavyAttack:
                {
                    events.Evt_InputCastSkill.Invoke(EInputActionType.HeavyAttack, keyPress.State == KeyState.Released);
                    break;
                }
            }
        }

        private void ApplyRollSkill(int id, ESkillDirection dir)
        {
            if (!Photon.ConnectedPlayers.TryGetValue(id, out var player))
            {
                Helpers.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            Helpers.Log($"Applying roll skill for player {id}");
            events.Evt_TriggerRollSkill.Invoke(dir);
        }

        public void DeInit()
        {
            Helpers.Log("DeInit");
        }
    }
}