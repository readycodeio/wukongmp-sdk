using System;
using CSharpModBase;
using UnrealEngine.Engine;

namespace WukongCSharpMod
{
    public class ModActor : AActor
    {
        protected override void ReceiveBeginPlay_Implementation()
        {
            base.ReceiveBeginPlay_Implementation();
            
            Console.WriteLine("ReceiveBeginPlay_Implementation");

            var playerController = World.GetPlayerController(0);
            
            if (playerController != null && playerController.GetControlledPawn() != null)
            {
                // Get the player's pawn (actor controlled by the player)
                var playerActor = playerController.GetControlledPawn();
                Log.Info($"Player Actor: {playerActor.GetName()}");
            }
            else
            {
                Log.Warn("Player controller or pawn not found.");
            }
        }
    }
}
