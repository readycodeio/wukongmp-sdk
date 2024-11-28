using System;
using System.Collections.Generic;
using System.Reflection;
using b1;
using BtlShare;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Plugins.EnhancedInput;
using UnrealEngine.Runtime;
using WukongMp.Common;
using FInputActionValue = b1.FInputActionValue;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        private WukongClient _photon;
        private readonly Harmony _harmony = new Harmony("WukongMP");

        // public BUS_MovementSystem CloneMovementSystem { get; private set; }

        private readonly Dictionary<int, PlayerState> _connectedPlayers = new Dictionary<int, PlayerState>();

        public void Init()
        {
            Console.WriteLine("Init");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Console.WriteLine("Patched with Harmony");

            _photon = new WukongClient();
            _photon.StartClient();

            // _photon.OnPlayerMoved += MoveClone;
            _photon.OnPlayerJoined += id => Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id));
            _photon.OnKeyReceived += ApplyPlayerInput;
            _photon.OnPlayerPosition += ApplyPlayerPosition;

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.Z, () =>
            {
                Console.WriteLine("Alt + Z");
                _photon.Reconnect();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Console.WriteLine("Alt + X");
                SpawnPlayersAlreadyInRoom();
            });
        }

        private void SpawnPlayersAlreadyInRoom()
        {
            // when joining game, spawn all players already in room
            foreach (var id in _photon.GetOtherPlayersInRoom())
            {
                SpawnCloneForJoiningPlayer(id);
            }
        }

        private void SpawnCloneForJoiningPlayer(int id)
        {
            if (_connectedPlayers.ContainsKey(id))
            {
                Console.WriteLine($"Player already exists: {id}");
                return;
            }

            var controller = GameUtils.GetPlayerController();
            var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
            var oldPawn = GameUtils.GetControlledPawn();
            var newTransform = oldPawn.GetActorTransform();
            newTransform.Translation -= oldPawn.GetActorForwardVector() * 400;

            BUS_EventCollectionCS.Get(oldPawn).Evt_TriggerInputActionImpl += SendInputEvents;

            BGUFuncLibPlayer.SpwanAndPossesPlayerContrlledPawn(controller, playerPawnClass, newTransform, pawn => { }, new BGUFuncLibPlayer.SpawnControlledPawnBlendParam
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
                case "IA_Walk":
                    key = PlayerInput.Walk;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_Sprint_KB":
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
            // var currentZ = clone.GetActorLocation().Z;

            // TODO: Set player.LastMovement
            var goal = new FVector(x, y, z);
            Console.WriteLine($"Moving to {goal}");
            events.Evt_AIMoveTo.Invoke(goal, null, player.MovementType, 10f, EBGUMoveAIType.None, false, false, "", "");
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