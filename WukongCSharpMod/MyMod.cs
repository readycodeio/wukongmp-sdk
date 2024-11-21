using System;
using b1;
using BtlShare;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.InputCore;
using UnrealEngine.Runtime;
using WukongMp.Common;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        private WukongClient _photon;
        private readonly Harmony _harmony = new Harmony("WukongMP");

        private APawn _clone;

        public void Init()
        {
            Console.WriteLine("Init");

            _harmony.PatchAll();

            _photon = new WukongClient();
            _photon.StartClient();

            _photon.OnPlayerMoved += MoveClone;
            _photon.OnKeyReceived += ApplyKeyPress;

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            {
                Console.WriteLine("Alt + V");

                var controller = GameUtils.GetPlayerController();
                var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
                var oldPawn = GameUtils.GetControlledPawn();
                var newTransform = oldPawn.GetActorTransform();
                newTransform.Translation += oldPawn.GetActorForwardVector() * 200;

                BGUFuncLibPlayer.SpwanAndPossesPlayerContrlledPawn(controller, playerPawnClass, newTransform, pawn => { }, new BGUFuncLibPlayer.SpawnControlledPawnBlendParam
                {
                    NeedBlend = false
                });

                // BGU_UnrealWorldUtil.DestroyActor(oldPawn);
                _clone = oldPawn;

                var cloneCharacter = _clone as BGUPlayerCharacterCS;

                FActorSpawnParameters spawnInfo = new FActorSpawnParameters
                {
                    Instigator = cloneCharacter.GetInstigator(),
                    SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod.AlwaysSpawn,
                    OverrideLevel = cloneCharacter.GetLevel(),
                    ObjectFlags = EObjectFlags.Transient // We never want to save AI controllers into a map
                };

                var loc = cloneCharacter.GetActorLocation();
                var rot = cloneCharacter.GetActorRotation();

                var @class = UClass.GetClass("BGPPlayerController"); // "BGPPlayerController" works for sure

                if (@class is null)
                {
                    Console.WriteLine("Class is null");
                    return;
                }

                var newController = GameUtils.GetWorld().SpawnActor(@class, ref loc, ref rot, ref spawnInfo);

                Console.WriteLine("Spawned new controller");

                if (newController != null && newController is ABGPPlayerController ctrl)
                {
                    ctrl.Possess(_clone);
                    Console.WriteLine("Possessed new controller");

                    ctrl.InitInputSystemCS();
                    ctrl.EnableClickEvents = true;
                }
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Console.WriteLine("Alt + X");

                _clone.GetMovementComponent().StopActiveMovement();
            });
        }

        private void ApplyKeyPress(int id, ConsoleKey key)
        {
            if (!(_clone.GetController() is ABGPPlayerController controller))
            {
                Console.WriteLine("Controller is null");
                return;
            }

            switch (key)
            {
                case ConsoleKey.Spacebar:
                    BUS_EventCollectionCS.Get(_clone).Evt_TriggerJumpSkill.Invoke(ESkillDirection.None, FVector2D.ZeroVector);
                    break;
            }
        }

        private void MoveClone(int id, float x, float y, float z)
        {
            var vec = new FVector(x, y, z);
            var events = BUS_EventCollectionCS.Get(_clone);

            var goal = _clone.GetActorTransform();
            goal.SetLocation(goal.GetLocation() + vec);

            vec.ToDirectionAndLength(out var dir, out var mag);
            // events.Evt_StopCurrentMove.Invoke();
            
            // events.Evt_InputMoveForward.Invoke(mag);

            // events.Evt_MatchingPositionMove.Invoke(new FMatchingPositionMoveParam
            // {
            //     TargetTrans = goal,
            //     bFacingTargetRotation = false,
            //     AcceptableRadius = 10f,
            //     MatchingPosType = EMatchingPosType.InterpolationLiner,
            //     MoveSpeedType = EAIMoveSpeedType.RUN,
            //     InterpMoveCallbackFunc = done =>
            //     {
            //         Console.WriteLine("Move done: " + done);
            //         events.Evt_ClearMoveToTarget.Invoke();
            //     }
            // });
        }

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