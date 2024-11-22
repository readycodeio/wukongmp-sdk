using System;
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

        public static APawn Clone { get; private set; }

        public void Init()
        {
            Console.WriteLine("Init");

            _harmony.PatchAll();

            _photon = new WukongClient();
            _photon.StartClient();

            // _photon.OnPlayerMoved += MoveClone;
            _photon.OnKeyReceived += ApplyKeyPress;

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.Z, () =>
            {
                Console.WriteLine("Alt + Z");
                _photon.Reconnect();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            {
                Console.WriteLine("Alt + V");

                var controller = GameUtils.GetPlayerController();
                var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
                var oldPawn = GameUtils.GetControlledPawn();
                var newTransform = oldPawn.GetActorTransform();
                newTransform.Translation += oldPawn.GetActorForwardVector() * 200;

                BUS_EventCollectionCS.Get(oldPawn).Evt_TriggerInputActionImpl += LogInputEvents;

                BGUFuncLibPlayer.SpwanAndPossesPlayerContrlledPawn(controller, playerPawnClass, newTransform, pawn => { }, new BGUFuncLibPlayer.SpawnControlledPawnBlendParam
                {
                    NeedBlend = false
                });

                // BGU_UnrealWorldUtil.DestroyActor(oldPawn);
                Clone = oldPawn;

                var cloneCharacter = Clone as BGUPlayerCharacterCS;

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
                    ctrl.Possess(Clone);
                    Console.WriteLine("Possessed new controller");
                }
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Console.WriteLine("Alt + X");

                Clone.GetMovementComponent().StopActiveMovement();
            });
        }

        private void LogInputEvents(string actionname, ETriggerEvent triggerevent, FInputActionValue value)
        {
            Console.WriteLine($"Action: {actionname}, TriggerEvent: {triggerevent}, Value: {value}");
        }

        private void ApplyKeyPress(int id, KeyPress keyPress)
        {
            if (!(Clone.GetController() is ABGUAIPlayerController controller))
            {
                Console.WriteLine("Controller is null");
                return;
            }

            var events = BUS_EventCollectionCS.Get(Clone);

            switch (keyPress.Key)
            {
                case ConsoleKey.W when keyPress.State == KeyState.Held:
                    events.Evt_InputMoveForward.Invoke(10000f);
                    break;
                case ConsoleKey.A when keyPress.State == KeyState.Held:
                    events.Evt_RepPlayerLoc.Invoke(Clone.GetActorLocation() + new FVector(0, 0, 100), "self");
                    break;
                case ConsoleKey.S when keyPress.State == KeyState.Held:
                    events.Evt_InputMoveForward.Invoke(-10000f);
                    break;
                case ConsoleKey.D when keyPress.State == KeyState.Held:
                    events.Evt_InputMoveRight.Invoke(10000f);
                    break;
                case ConsoleKey.Spacebar when keyPress.State == KeyState.Pressed:
                    events.Evt_TriggerJumpSkill.Invoke(ESkillDirection.None, FVector2D.ZeroVector);
                    break;
                case ConsoleKey.J:
                    events.Evt_InputCastSkill.Invoke(EInputActionType.LightAttack, keyPress.State == KeyState.Released);
                    break;
                // case ConsoleKey.W:
                //     events.Evt_TriggerInputActionImpl.Invoke(
                //         "IA_B1MoveForward",
                //         keyPress.State == KeyState.Released ? ETriggerEvent.Completed : ETriggerEvent.Triggered,
                //         keyPress.State == KeyState.Released ? FInputActionValue.False : FInputActionValue.Forward
                //     );
                //     break;
                // case ConsoleKey.S:
                //     events.Evt_TriggerInputActionImpl.Invoke(
                //         "IA_B1MoveForward",
                //         keyPress.State == KeyState.Released ? ETriggerEvent.Completed : ETriggerEvent.Triggered,
                //         keyPress.State == KeyState.Released ? FInputActionValue.False : FInputActionValue.Backward
                //     );
                //     break;
                // case ConsoleKey.A:
                //     events.Evt_TriggerInputAction.Invoke(
                //         "IA_B1MoveSideways",
                //         keyPress.State == KeyState.Released ? ETriggerEvent.Completed : ETriggerEvent.Triggered,
                //         keyPress.State == KeyState.Released ? FInputActionValue.False : FInputActionValue.Left
                //     );
                //     break;
                // case ConsoleKey.D:
                //     events.Evt_TriggerInputAction.Invoke(
                //         "IA_B1MoveSideways",
                //         keyPress.State == KeyState.Released ? ETriggerEvent.Completed : ETriggerEvent.Triggered,
                //         keyPress.State == KeyState.Released ? FInputActionValue.False : FInputActionValue.Right
                //     );
                //     break;
                // case ConsoleKey.Spacebar:
                //     events.Evt_TriggerInputActionImpl.Invoke(
                //         "IA_B1Jump",
                //         keyPress.State == KeyState.Released ? ETriggerEvent.Completed : ETriggerEvent.Started,
                //         keyPress.State == KeyState.Released ? FInputActionValue.False : FInputActionValue.True
                //     );
                //     break;
                // case ConsoleKey.J:
                //     events.Evt_TriggerInputActionImpl.Invoke(
                //         "IA_B1LightAttack",
                //         keyPress.State == KeyState.Released ? ETriggerEvent.Completed : ETriggerEvent.Started,
                //         keyPress.State == KeyState.Released ? FInputActionValue.False : FInputActionValue.True
                //     );
                //     break;
            }
        }

        [HarmonyPatch(typeof(BUIAMove), "GetOwner")]
        [HarmonyPostfix]
        public static void PostTemplate(ref AActor __result)
        {
            if (Clone != null)
            {
                __result = Clone;
            }

            Console.WriteLine($"GetOwner: {__result?.GetName()}");
        }

        [HarmonyPatch(typeof(BGUFuncLibMap), "IsPartyLevel")]
        [HarmonyPostfix]
        public static void Post(ref bool __result)
        {
            __result = true;
        }

        // private void MoveClone(int id, float x, float y, float z)
        // {
        //     var vec = new FVector(x, y, z);
        //     var events = BUS_EventCollectionCS.Get(_clone);
        //
        //     var goal = _clone.GetActorTransform();
        //     goal.SetLocation(goal.GetLocation() + vec);
        //
        //     vec.ToDirectionAndLength(out var dir, out var mag);
        //     // events.Evt_StopCurrentMove.Invoke();
        //     
        //     events.Evt_TriggerInputActionImpl.Invoke("", ETriggerEvent.Started);
        //
        //     // events.Evt_InputMoveForward.Invoke(x);
        //     // events.Evt_InputMoveRight.Invoke(y);
        //
        //     // if (x > 0)
        //     // {
        //     //     events.Evt_AnyKeyInput.Invoke(false, new FKey(EKeys.W)); // nie działa
        //     // }
        //     // else if (x < 0)
        //     // {
        //     //     events.Evt_SetMovementInput.Invoke(dir, mag, false); // działa
        //     // }
        //     // else
        //     // {
        //     //     events.Evt_AISideWalk.Invoke(x, y); // działa
        //     // }
        //
        //     // events.Evt_MatchingPositionMove.Invoke(new FMatchingPositionMoveParam
        //     // {
        //     //     TargetTrans = goal,
        //     //     bFacingTargetRotation = false,
        //     //     AcceptableRadius = 10f,
        //     //     MatchingPosType = EMatchingPosType.InterpolationLiner,
        //     //     MoveSpeedType = EAIMoveSpeedType.JOG,
        //     //     InterpMoveCallbackFunc = done => { Console.WriteLine("Move done: " + done); },
        //     //     AIPathMoveCallbackFunc = done => { Console.WriteLine("Path move done: " + done); },
        //     //     bIncludeSelfRadius = false,
        //     //     InterpMoveTime = 0f
        //     // });
        // }

        // private void MoveMonstersInRange(int id, float x, float y, float z)
        // {
        //     var playerCharacter = GameUtils.GetBguPlayerCharacterCs();
        //
        //     var pawn = playerCharacter.GetController().GetControlledPawn();
        //     var playerLoc = pawn.GetActorTransform().GetLocation();
        //
        //     foreach (var monster in GameUtils.GetMonsters())
        //     {
        //         var controller = monster.GetController();
        //
        //         if (controller is null)
        //             continue;
        //
        //         // x is forward / backward, y is left / right
        //         var forward = monster.GetActorForwardVector();
        //         var left = forward.Cross_VectorVector(FVector.UpVector);
        //
        //         var goal = playerLoc + forward * x - left * y;
        //         Console.WriteLine("Requested move to: " + goal + " for monster " + monster.GetName());
        //
        //         // UAIHelperLibrary.SimpleMoveToLocation(controller, goal);
        //         BGUFuncLibAICS.BGUCancelAICurrentMove(monster);
        //         BGUFuncLibAICS.BGURequestAIMoveToLocation(monster, goal, EAIMoveSpeedType.JOG, 10, EBGUMoveAIType.KeepFacingTarget, false, false);
        //     }
        // }

        public void DeInit()
        {
            Console.WriteLine("DeInit");
        }
    }
}