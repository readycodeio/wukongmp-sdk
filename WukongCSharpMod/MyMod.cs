using System;
using System.Collections.Generic;
using System.Reflection;
using b1;
using BtlShare;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using UnrealEngine.AIModule;
using UnrealEngine.Engine;
using UnrealEngine.Plugins.EnhancedInput;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Common;
using FInputActionValue = b1.FInputActionValue;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        private UUserWidget _chatWidget;

        private WukongClient _photon;
        private readonly Harmony _harmony = new Harmony("WukongMP");

        private readonly Dictionary<int, PlayerState> _connectedPlayers = new Dictionary<int, PlayerState>();
        private readonly Dictionary<byte, MonsterState> _monsters = new Dictionary<byte, MonsterState>();

        private FVector _savedPosition;

        public void Init()
        {
            Console.WriteLine("Init");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Console.WriteLine("Patched with Harmony");

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.Z, () =>
            {
                Console.WriteLine("Alt + Z");
                _photon.Reconnect();
            });

            InitWorldCallbacks();

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Console.WriteLine("Alt + X");

                InitPhoton();
                Connect();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Console.WriteLine("Alt + H");

                InitializeChatWidget();
                InitPhoton();
            });
        }

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
                Console.WriteLine("World is null.");
            }
        }

        private void InitPhoton()
        {
            var myLocation = GameUtils.GetControlledPawn().GetActorTransform().GetLocation();
            _photon = new WukongClient(SpawnPlayersAlreadyInRoom, myLocation.X, myLocation.Y, myLocation.Z);
            _photon.WukongChat.OnGetMessage += GetMessageFromWidget;
            _photon.WukongChat.OnConnectRequest += Connect;
        }

        private void OnMapLoaded()
        {
            UWorld world = GameUtils.GetWorld();
            if (world != null)
                Console.WriteLine($"Map loaded: {world.GetCurrentLevelName()}");
        }

        private void OnDelayBeginPlay()
        {
            Console.WriteLine("Delay begin play.");
            InitializeChatWidget();
            InitPhoton();
        }

        private void Connect()
        {
            if (_photon.Ready)
            {
                return;
            }

            _photon.OnPlayerJoined += (id, x, y, z) => Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id, x, y, z));
            _photon.OnKeyReceived += (id, key) => Utils.TryRunOnGameThread(() => ApplyPlayerInput(id, key));
            _photon.OnPlayerPosition += (id, x, y, z) => Utils.TryRunOnGameThread(() => ApplyPlayerPosition(id, x, y, z));
            _photon.OnUnitSpawn += (_, id, name, x, y, z) => Utils.TryRunOnGameThread(() => SpawnRemoteUnit(id, name, x, y, z));
            _photon.WukongChat.OnSendMessage += AddMessageToWidget;
            _photon.WukongChat.OnSavePosition += SaveCurrentPosition;
            _photon.WukongChat.OnLoadPosition += LoadSavedPosition;
            _photon.WukongChat.OnSpawnEnemy += name => Utils.TryRunOnGameThread(() => SpawnEnemy(name));

            _photon.StartClient();

            var myPawn = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(myPawn);
            events.Evt_TriggerInputActionImpl += SendInputEvents;
        }

        private void SpawnEnemy(string unitName)
        {
            var controlledPawn = GameUtils.GetControlledPawn();
            var loc = controlledPawn.GetActorLocation() + new FVector(300, 300, 0);

            var id = (byte)_monsters.Count; // TODO: Overflow??
            var pawn = SpawnUnit(id, unitName, loc.X, loc.Y, loc.Z, false);

            _monsters[id] = new MonsterState
            {
                Id = id,
                Local = true,
                Pawn = pawn
            };

            _photon.SpawnUnit(id, unitName, loc.X, loc.Y, loc.Z);
        }

        private APawn SpawnRemoteUnit(byte id, string unitName, float x, float y, float z)
        {
            return SpawnUnit(id, unitName, x, y, z, true);
        }

        private APawn SpawnUnit(byte id, string unitName, float x, float y, float z, bool remote)
        {
            Console.WriteLine($"Spawn unit called for {unitName}");

            var loc = new FVector(x, y, z);
            var rot = new FRotator();

            var @class = UObject.LoadClass<AActor>(null, unitName);

            if (@class is null)
            {
                Console.WriteLine("Enemy class is null");
                return null;
            }

            Console.WriteLine($"Class to spawn: {@class.PathName}");

            var transform = new FTransform(rot, loc);
            var actor = BGU_UnrealActorUtil.BGUBeginDeferredActorSpawnFromClass(GameUtils.GetWorld(), @class, transform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null) as APawn;
            BGU_UnrealActorUtil.BGUFinishSpawningActor(actor, transform);

            if (!remote)
                return actor;

            _monsters.Add(id, new MonsterState
            {
                Id = id,
                Local = false,
                Pawn = actor
            });

            // de-brain AI
            var controller = actor.GetController();

            if (controller is null)
            {
                Console.WriteLine("No controller");
                return actor;
            }

            Console.WriteLine("Has controller");

            var ai = controller.Cast<AIController>();

            if (ai is null)
            {
                Console.WriteLine("No AI");
                return actor;
            }

            Console.WriteLine("Has AI");

            var brain = ai.BrainComponent;

            if (brain is null)
            {
                Console.WriteLine("No brain");
                return actor;
            }

            Console.WriteLine("Has brain");
            brain.StopLogic("Stop");

            return actor;
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
            var localTransform = GameUtils.GetControlledPawn().GetActorTransform().GetLocation();

            // when joining game, spawn all players already in room
            foreach (var id in _photon.GetOtherPlayersInRoom())
            {
                Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id, localTransform.X, localTransform.Y, localTransform.Z));
            }
        }

        private void SpawnCloneForJoiningPlayer(int id, float x, float y, float z)
        {
            if (_connectedPlayers.ContainsKey(id))
            {
                Console.WriteLine($"Player already exists: {id}");
                return;
            }

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
                Console.WriteLine("Class is null");
                return;
            }

            var newController = GameUtils.GetWorld().SpawnActor(@class, ref loc, ref rot, ref spawnInfo);

            Console.WriteLine("Spawned new controller");

            if (newController != null && newController is ABGUAIPlayerController ctrl)
            {
                ctrl.Possess(clone);
                ctrl.CanBeDamaged = false;
                Console.WriteLine("Possessed new controller");
            }

            // assign in dictionary
            _connectedPlayers[id] = new PlayerState(id, clone);

            // teleport clone to cloneTransform
            var targetTransform = new FTransform(FRotator.ZeroRotator, new FVector(x, y, z));
            clone.SetActorTransform(targetTransform, false, out _, true);

            var controlledPawn = GameUtils.GetControlledPawn();
            controlledPawn.SetActorTransform(new FTransform(oldPawnRot, oldPawnPos), false, out _, false);
        }

        private void SendInputEvents(string actionname, ETriggerEvent triggerevent, FInputActionValue value)
        {
            KeyState keyState;
            PlayerInput key;

            Console.WriteLine($"Action: {actionname}, TriggerEvent: {triggerevent}, Value: {value}");

            switch (actionname)
            {
                case "IA_B1MoveForward":
                case "IA_B1MoveSideways":
                    if (triggerevent == ETriggerEvent.Triggered)
                    {
                        var transform = GameUtils.GetControlledPawn().BGUGetActorTransform();
                        var pos = transform.GetLocation();
                        _photon.SendPositionUpdate(pos.X, pos.Y, pos.Z);
                    }
                    else if (triggerevent == ETriggerEvent.Completed)
                    {
                        // TODO: stopped moving, set to idle? (not really, 2 keys can be held at the same time)
                    }

                    return;
                case "IA_B1Jump":
                    key = PlayerInput.Jump;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1Roll_KB":
                    key = PlayerInput.Roll;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1LightAttack":
                    key = PlayerInput.LightAttack;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1HeavyAttack":
                    key = PlayerInput.HeavyAttack;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1Spell_QS":
                    key = PlayerInput.CastImmobilize;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1Walk":
                    key = PlayerInput.Walk;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1Sprint_KB":
                    key = PlayerInput.Sprint;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                default:
                    return;
            }

            _photon.SendKeyPressed(key, keyState);
        }

        private void ApplyPlayerPosition(int id, float x, float y, float z)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                Console.WriteLine($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            // TODO: Set player.LastMovement
            var goal = new FVector(x, y, z);
            try
            {
                events.Evt_AIMoveTo.Invoke(goal, null, player.MovementType, 10f, EBGUMoveAIType.None, false, false, "", "");
            }
            catch
            {
                // ignore
            }
        }

        private void AddMessageToWidget(bool isServerMesssage, string sender, string message)
        {
            if (_chatWidget != null)
            {
                Console.WriteLine($"Calling AddMessage function with message {message} from {sender}");
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
                    Console.WriteLine($"Got message: {message} in GetSentMessage funcition");
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
                    Console.WriteLine("Chat widget initialized!.");
                }
            }
        }

        private void ApplyPlayerInput(int id, KeyPress keyPress)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                Console.WriteLine($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;

            var events = BUS_EventCollectionCS.Get(clone);

            Console.WriteLine($"Player {id} pressed key {keyPress.Key} with state {keyPress.State}");

            switch (keyPress.Key)
            {
                case PlayerInput.Jump when keyPress.State == KeyState.Pressed:
                    // TODO: Direction
                    events.Evt_TriggerJumpSkill.Invoke(player.LastMovement, FVector2D.ZeroVector);
                    break;
                case PlayerInput.LightAttack:
                {
                    var prop = events.GetType().GetProperty("Evt_InputCastSkill");
                    var gottenPropObj = prop.GetValue(events);
                    var method = gottenPropObj.GetType().GetMethod("Invoke");
                    method.Invoke(gottenPropObj, new object[] { EInputActionType.LightAttack, keyPress.State == KeyState.Released, 0, -1, -1 });
                    break;
                }
                case PlayerInput.HeavyAttack:
                {
                    var prop = events.GetType().GetProperty("Evt_InputCastSkill");
                    var gottenPropObj = prop.GetValue(events);
                    var method = gottenPropObj.GetType().GetMethod("Invoke");
                    method.Invoke(gottenPropObj, new object[] { EInputActionType.HeavyAttack, keyPress.State == KeyState.Released, 0, -1, -1 });
                    break;
                }
                // TODO: This doesn't yet work
                case PlayerInput.CastImmobilize:
                    events.Evt_CastDingShenToTarget.Invoke(new FGSCastDingShenSetting
                    {
                        RangeRadius = 1000,
                        SelectCount = 1,
                    }, 1000f);
                    break;
                case PlayerInput.Roll:
                    events.Evt_TriggerRollSkill.Invoke(player.LastMovement);
                    break;
                case PlayerInput.Walk:
                    player.MovementType = keyPress.State == KeyState.Pressed ? EAIMoveSpeedType.JOG : EAIMoveSpeedType.RUN;
                    break;
                case PlayerInput.Sprint:
                    player.MovementType = keyPress.State == KeyState.Pressed ? EAIMoveSpeedType.SPRINT : EAIMoveSpeedType.RUN;
                    break;
            }
        }

        public void DeInit()
        {
            Console.WriteLine("DeInit");
        }
    }
}