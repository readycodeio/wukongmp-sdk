using System;
using System.Diagnostics;
using b1;
using CSharpModBase;
using CSharpModBase.Input;
using UnrealEngine.AIModule;
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

        public void Init()
        {
            Console.WriteLine("Init");

            _photon = new WukongClient();
            _photon.StartClient();

            _photon.OnPlayerMoved += MoveMonstersInRange;

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Console.WriteLine("Alt + X");

                var playerCharacter = GameUtils.GetBguPlayerCharacterCs();
                if (playerCharacter != null)
                {
                    var pawn = playerCharacter.GetController().GetControlledPawn();
                    var t = pawn.GetActorTransform();
                    var loc = t.GetLocation();
                    loc.Z += 1000f;
                    t.SetLocation(loc);
                    if (!pawn.SetActorTransform(t, true, out _, true))
                    {
                        Debug.WriteLine("Failed to teleport player.");
                    }
                }
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.Z, () =>
            {
                Console.WriteLine("Alt + Z");

                foreach (var monster in GameUtils.GetMonsters())
                {
                    try
                    {
                        Console.WriteLine($"Monster: {monster.GetName()}");

                        var controller = monster.GetController();

                        if (controller is null)
                            continue;

                        Console.WriteLine("Has controller");

                        var ai = controller.Cast<AIController>();

                        if (ai is null)
                            continue;

                        Console.WriteLine("Has AI");

                        var brain = ai.BrainComponent;

                        if (brain is null)
                            continue;

                        Console.WriteLine("Has brain");

                        brain.StopLogic("Stop");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });
        }

        private void MoveMonstersInRange(int id, float x, float y, float z)
        {
            var playerCharacter = GameUtils.GetBguPlayerCharacterCs();

            var pawn = playerCharacter.GetController().GetControlledPawn();
            var playerLoc = pawn.GetActorTransform().GetLocation();

            foreach (var monster in GameUtils.GetMonsters())
            {
                var controller = monster.GetController();

                if (controller is null)
                    continue;

                // x is forward / backward, y is left / right
                var forward = monster.GetActorForwardVector();
                var left = forward.Cross_VectorVector(FVector.UpVector); // FVector from Unreal Engine

                var goal = playerLoc + forward * x + left * y;
                Console.WriteLine("Requested move to: " + goal);

                // UAIHelperLibrary.SimpleMoveToLocation(controller, goal);
                BGUFuncLibAICS.BGUCancelAICurrentMove(monster);
                BGUFuncLibAICS.BGURequestAIMoveToLocationWithMM(monster, goal, EAIMoveSpeedType.JOG, 10, EBGUMoveAIType.None, false, false, EState_MM.FreeWalk);
            }
        }

        public void DeInit()
        {
            Console.WriteLine("DeInit");
        }
    }
}