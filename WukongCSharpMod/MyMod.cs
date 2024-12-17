using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using b1;
using b1.UI;
using BtlShare;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
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

        private UUserWidget chatWidget;

        private WukongClient _photon;
        private readonly Harmony _harmony = new Harmony("WukongMP");

        // public BUS_MovementSystem CloneMovementSystem { get; private set; }

        private readonly Dictionary<int, PlayerState> _connectedPlayers = new Dictionary<int, PlayerState>();

        private FVector savedPosition;

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

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Console.WriteLine("Alt + X");

                var myLocation = GameUtils.GetControlledPawn().GetActorTransform().GetLocation();

                _photon = new WukongClient(SpawnPlayersAlreadyInRoom, myLocation.X, myLocation.Y, myLocation.Z);
                _photon.StartClient();

                _photon.OnPlayerJoined += (id, x, y, z) => Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id, x, y, z));
                _photon.OnKeyReceived += (id, key) => Utils.TryRunOnGameThread(() => ApplyPlayerInput(id, key));
                _photon.OnPlayerPosition += (id, x, y, z) => Utils.TryRunOnGameThread(() => ApplyPlayerPosition(id, x, y, z));
                _photon.WukongChat.OnSendMessage += AddMessageToWidget;
                _photon.WukongChat.OnGetMessage += GetMessageFromWidget;
                _photon.WukongChat.OnSavePosition += SaveCurrentPosition;
                _photon.WukongChat.OnLoadPosition += LoadSavedPosition;
                _photon.WukongChat.OnSpawnEnemy += (name) => Utils.TryRunOnGameThread(() => SpawnEnemy(name));
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Console.WriteLine("Alt + H");
                InitializeChatWidget();
            });
        }

        private void SpawnEnemy(string obj)
        {
            APawn controlledPawn = GameUtils.GetControlledPawn();
            var loc = controlledPawn.GetActorLocation() + new FVector(300, 300,0 );
            var rot = controlledPawn.GetActorRotation();

            var @class = UClass.LoadClass<AActor>(null, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_02.TAMER_gycy_lang_02_C");

            if (@class is null)
            {
                Console.WriteLine("Enemy class is null");
                return;
            }
            else
            {
                Console.WriteLine($"Class to spawn: {@class.PathName}");
            }

            FTransform transform = new FTransform(rot, loc);
            BUTamerActor tamer = BGU_UnrealActorUtil.BGUBeginDeferredActorSpawnFromClass(GameUtils.GetWorld(), @class, transform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null) as BUTamerActor;
            BGU_UnrealActorUtil.BGUFinishSpawningActor(tamer, transform);
            var monster = tamer.GetMonster();
            if (monster != null)
            {
                Console.WriteLine($"Moster class: {monster.PathName}");
            }
            else
            {
                Console.WriteLine("Monster not spawned");
            }

            if (tamer != null)
            {
                Console.WriteLine("Enemy spawned");
            }
        }

        private void LoadSavedPosition()
        {
            APawn pawn = GameUtils.GetControlledPawn();
            if (pawn != null)
            {
                pawn.SetActorLocation(savedPosition, false, out _, true);
            }
        }

        private void SaveCurrentPosition()
        {
            APawn pawn = GameUtils.GetControlledPawn();
            if (pawn != null)
            {
                savedPosition = pawn.GetActorLocation();
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

            var cloneTransform = oldPawn.GetActorTransform();
            cloneTransform.Translation = new FVector(x, y, z);

            BUS_EventCollectionCS.Get(oldPawn).Evt_TriggerInputActionImpl += SendInputEvents;

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
                // ctrl.CanBeDamaged = false;
                Console.WriteLine("Possessed new controller");
            }

            // assign in dictionary
            _connectedPlayers[id] = new PlayerState(id, clone);

            // teleport clone to cloneTransform
            clone.SetActorTransform(cloneTransform, false, out _, true);
        }

        private void SendInputEvents(string actionname, ETriggerEvent triggerevent, FInputActionValue value)
        {
            Console.WriteLine($"SendInputEvents: {actionname} {triggerevent} {value}");

            KeyState keyState;
            PlayerInput key;

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
            if (chatWidget != null)
            {
                Console.WriteLine($"Calling AddMessage funcition with message {message} from {sender}");
                chatWidget.CallFunctionByNameWithArguments($"AddMessage {isServerMesssage} {sender} {message}", true);
            }
            else
            {
                InitializeChatWidget();
            }
        }

        private string GetMessageFromWidget()
        {
            if (chatWidget != null)
            {
                chatWidget.CallFunctionByNameWithArguments("GetSentMessage", true);
                var message = chatWidget.ToolTipText.ToString();
                if (message.Length > 0)
                {
                    Console.WriteLine($"Got message: {message} in GetSentMessage funcition");
                }
                return message;
            }
            else
            {
                InitializeChatWidget();
            }
            return "";
        }

        private void InitializeChatWidget()
        {
            var widgets = GameUtils.GetWidgets();
            if (widgets != null)
            {
                if (widgets.Count == 1)
                {
                    chatWidget = widgets[0];
                    Console.WriteLine($"Chat widget initialzied!.");
                }
                //else
                //{
                //    Console.WriteLine($"Error!, Found {widgets.Count} widgets. Expected 1.");
                //}
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

            switch (keyPress.Key)
            {
                case PlayerInput.Jump when keyPress.State == KeyState.Pressed:
                    // TODO: Direction
                    events.Evt_TriggerJumpSkill.Invoke(player.LastMovement, FVector2D.ZeroVector);
                    break;
                case PlayerInput.LightAttack:
                    events.Evt_InputCastSkill.Invoke(EInputActionType.LightAttack, keyPress.State == KeyState.Released);
                    break;
                case PlayerInput.HeavyAttack:
                    events.Evt_InputCastSkill.Invoke(EInputActionType.HeavyAttack, keyPress.State == KeyState.Released);
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