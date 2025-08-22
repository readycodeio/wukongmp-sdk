using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils
{
    public static class GameUtils
    {
        private static UWorld? _world;

        public static UWorld? GetWorld()
        {
            if (_world == null)
            {
                var obj = GCHelper.FindRef(FGlobals.GWorld)?.Managed;
                _world = (obj is UWorld ? obj : null) as UWorld;
            }

            return _world;
        }

        public static BGUPlayerCharacterCS? GetControlledPawn()
        {
            var pawn = UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld())?.GetControlledPawn() as BGUPlayerCharacterCS;
            return pawn.IsNullOrDestroyed() ? null : pawn;
        }

        public static BGP_PlayerControllerB1 GetPlayerController()
        {
            return (BGP_PlayerControllerB1)UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld());
        }

        public static bool IsGameInstanceValid() => BGWGameInstanceCS.Get(null) != null;

        public static bool IsWorldValid() => GetWorld() != null;

        public static void PossesPawnWithViewTarget(ABGPPlayerController controller, APawn possessPawn, APawn unpossessPawn, FRotator controllerRotation)
        {
            PossessPawn(controller, possessPawn, unpossessPawn);
            controller.SetViewTargetWithBlend(possessPawn);
            controller.SetControlRotation(controllerRotation);
        }

        public static void PossessPawn(ABGPPlayerController controller, APawn possessPawn, APawn unpossessPawn)
        {
            controller.Possess(possessPawn);
            BPS_GSEventCollection.Get(controller).Evt_BPS_OnControlledPawnChange.Invoke(possessPawn);
            BGS_EventCollectionCS.Get(controller)?.Evt_NotifyPossessEntityChanged.Invoke(unpossessPawn.ToEntity(), possessPawn.ToEntity());
        }

        public static void EnableThreading()
        {
            Logging.LogDebug("Enabling threading for ECSWorld");
            BGW_ECSWorld.ThreadCount = 4;
        }

        public static void DisableThreading()
        {
            Logging.LogDebug("Disabling threading for ECSWorld");
            BGW_ECSWorld.ThreadCount = 0;
        }
    }
}
