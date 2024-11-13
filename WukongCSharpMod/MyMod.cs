using System;
using System.Diagnostics;
using System.Numerics;
using CSharpModBase;
using CSharpModBase.Input;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Common;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        private WukongClient photon;

        public void Init()
        {
            Console.WriteLine("Init");

            photon = new WukongClient();
            photon.StartClient();

            photon.OnPlayerMoved += MoveMonstersInRange;

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
                    var controller = monster.GetController();

                    if (controller is null)
                        continue;

                    Console.WriteLine("Has controller");

                    controller.UnPossess();
                }
            });
        }

        private void MoveMonstersInRange(int id, float x, float y, float z)
        {
            foreach (var monster in GameUtils.GetMonsters())
            {
                var t = monster.GetActorTransform();

                var loc = t.GetLocation();

                // x is forward / backward, y is left / right
                var forward = t.GetRotation().GetNormalized().Vector(); // FQuat from Unreal Engine
                var right = forward.Cross_VectorVector(FVector.UpVector); // FVector from Unreal Engine

                t.SetLocation(loc + forward * x + right * y);

                if (!monster.SetActorTransform(t, false, out _, false))
                {
                    Console.WriteLine("Failed to move monster.");
                }
            }
        }

        public void DeInit()
        {
            Console.WriteLine("DeInit");
        }
    }
}