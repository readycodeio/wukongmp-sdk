using System;
using b1;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using UnrealEngine.Engine;
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
            });

            // Utils.RegisterKeyBind(ModifierKeys.Alt, Key.Z, () =>
            // {
            //     Console.WriteLine("Alt + Z");
            //
            //     foreach (var monster in GameUtils.GetMonsters())
            //     {
            //         try
            //         {
            //             Console.WriteLine($"Monster: {monster.GetName()}");
            //
            //             var controller = monster.GetController();
            //
            //             if (controller is null)
            //                 continue;
            //
            //             Console.WriteLine("Has controller");
            //
            //             var ai = controller.Cast<AIController>();
            //
            //             if (ai is null)
            //                 continue;
            //
            //             Console.WriteLine("Has AI");
            //
            //             var brain = ai.BrainComponent;
            //
            //             if (brain is null)
            //                 continue;
            //
            //             Console.WriteLine("Has brain");
            //
            //             brain.StopLogic("Stop");
            //         }
            //         catch (Exception e)
            //         {
            //             Console.WriteLine(e);
            //         }
            //     }
            // });
            //


            // Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            // {
            //     Console.WriteLine("Alt + C");
            //
            //     var playerCharacter = GameUtils.GetBguPlayerCharacterCs();
            //     if (playerCharacter != null)
            //     {
            //         try
            //         {
            //             var cbi = new FContinueBehaviorInfo
            //             {
            //                 CBT = EContinueBehaviorType.AnimationSyncing,
            //                 BeatbackMontage = playerCharacter.GetCurrentMontage()
            //             };
            //             BUS_EventCollectionCS.Get(playerCharacter).Evt_SummonSkillCastByPhantomRush.Invoke(1001101, cbi);
            //         }
            //         catch (Exception e)
            //         {
            //             Console.WriteLine(e);
            //         }
            //     }
            // });
            //
            // Utils.RegisterKeyBind(ModifierKeys.Alt, Key.W, () =>
            // {
            //     Console.WriteLine("Alt + W");
            //
            //     var playerCharacter = GameUtils.GetBguPlayerCharacterCs();
            //     if (playerCharacter != null)
            //     {
            //         try
            //         {
            //             BUS_EventCollectionCS.Get(playerCharacter).Evt_TriggerPhantomRush.Invoke(ESkillDirection.Forward);
            //         }
            //         catch (Exception e)
            //         {
            //             Console.WriteLine(e);
            //         }
            //     }
            // });
        }

        private void MoveClone(int id, float x, float y, float z)
        {
            // AddMovementInput is a method of ACharacter
            var movement = _clone?.GetMovementComponent() as UBGUCharacterMovementComponent;

            if (movement is null)
            {
                Console.WriteLine("Movement is null");
                return;
            }

            var controller = _clone.GetController() as BGP_PlayerControllerCS;

            if (controller == null)
            {
                (_clone as BGUPlayerCharacterCS).SpawnDefaultController();
                controller = _clone.GetController() as BGP_PlayerControllerCS;

                if (controller is null)
                {
                    Console.WriteLine("Controller is null and cannot be spawned");
                    return;
                }

                controller.InitAllComp();
            }


            var translation = new FVector(x, y, z);
            var goal = _clone.GetActorLocation() + translation;
            var dir = translation.Rotation().GetNormalized();

            // TODO: Move the controller instead of teleporting the Pawn
            BUS_EventCollectionCS.Get(_clone).Evt_InterpolationMove.Invoke(goal, dir, 0.5f, true, false, false, true);
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