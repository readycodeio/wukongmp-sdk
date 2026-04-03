using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.FreeCamera;

namespace WukongMp.Api.WukongUtils;

/// <summary>
/// Provides utility methods for directly interacting with the game world, bypassing the SDK.
/// </summary>
/// <remarks>
/// These methods are not guaranteed to be stable across game updates, and might be removed in the future.
/// </remarks>
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

    public static BGP_PlayerControllerB1? GetPlayerController()
    {
        return (BGP_PlayerControllerB1?)UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld());
    }

    internal static void PossesPawnWithViewTarget(FreeCameraManager freeCameraManager, ABGPPlayerController controller, APawn possessPawn, APawn unpossessPawn, FRotator controllerRotation)
    {
        PossessPawn(controller, possessPawn, unpossessPawn);
        controller.SetViewTargetWithBlend(possessPawn);
        controller.SetControlRotation(controllerRotation);
        freeCameraManager.ReEnableFreeCamera();
    }

    internal static void PossessPawn(ABGPPlayerController controller, APawn possessPawn, APawn unpossessPawn)
    {
        controller.Possess(possessPawn);
        BPS_GSEventCollection.Get(controller).Evt_BPS_OnControlledPawnChange.Invoke(possessPawn);
        BGS_EventCollectionCS.Get(controller)?.Evt_NotifyPossessEntityChanged.Invoke(unpossessPawn.ToEntity(), possessPawn.ToEntity());
    }
}