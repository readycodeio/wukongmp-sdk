using b1;
using b1.BGW;
using System;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.FreeCamera;

public class FreeCameraManager
{
    private bool _isInFreeCameraMode;
    private BGUCharacterCS? _cachePlayerPawn;
    private AActor? _freeCameraActor;
    private float _gameFov;
    private AActor? _cacheCameraViewTarget;
    private const string FreeCameraActorPath = "/Game/Mods/WukongMod/BP_FreeCameraActor.BP_FreeCameraActor_C";

    public bool IsInFreeCameraMode => _isInFreeCameraMode;

    public event Action<bool>? OnFreeCameraModeChanged;

    public void EnterFreeCameraMode()
    {
        var world = GameUtils.GetWorld();
        if (world == null)
        {
            return;
        }

        if (_isInFreeCameraMode)
        {
            return;
        }

        var aBGPPlayerController = UGSE_EngineFuncLib.GetFirstLocalPlayerController(world) as ABGPPlayerController;
        if (aBGPPlayerController.IsNullOrDestroyed())
        {
            Logging.LogError("[FreeCameraManager] EnterFreeCameraMode PlayerController IsNull");
            return;
        }

        _cachePlayerPawn = aBGPPlayerController.GetControlledPawn() as BGUCharacterCS;
        if (_cachePlayerPawn.IsNullOrDestroyed())
        {
            Logging.LogError("[FreeCameraManager] EnterFreeCameraMode PlayerPawn IsNull");
            return;
        }

        var localPlayerCameraManager = UGSE_EngineFuncLib.GetLocalPlayerCameraManager(world);
        if (localPlayerCameraManager.IsNullOrDestroyed())
        {
            Logging.LogError("[FreeCameraManager] EnterFreeCameraMode PlayerCameraManager IsNull");
            return;
        }

        BGW_EnhancedInputMgrV2 bGW_EnhancedInputMgrV = BGW_EnhancedInputMgrV2.Get(world);
        if (bGW_EnhancedInputMgrV.InputModeTracker.InputMode != EGSInputMode.GameOnly)
        {
            Logging.LogError("[FreeCameraManager] Game is currently not in GameOnly mode");
            return;
        }

        var cameraLocation = localPlayerCameraManager.GetCameraLocation();
        var cameraRotation = localPlayerCameraManager.GetCameraRotation();
        if (_freeCameraActor.IsNullOrDestroyed())
        {
            var freeCameraActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(FreeCameraActorPath, ELoadResourceType.SyncLoadAndCache);
            if (freeCameraActorClass == null)
            {
                Logging.LogError("[FreeCameraManager] FreeCameraActor class is null");
                return;
            }

            _freeCameraActor = world.SpawnActor(freeCameraActorClass, ref cameraLocation, ref cameraRotation);
        }

        if (_freeCameraActor.IsNullOrDestroyed())
        {
            Logging.LogError("[FreeCameraManager] EnterFreeCameraMode Spawn FreeCameraActor Failed");
            return;
        }

        Logging.LogInformation("[FreeCameraManager] Entering free camera");
        _freeCameraActor.SetActorHiddenInGame(bNewHidden: false);
        _freeCameraActor.SetActorEnableCollision(bNewActorEnableCollision: true);
        _cacheCameraViewTarget = aBGPPlayerController.GetViewTarget();
        _gameFov = localPlayerCameraManager.GetFOVAngle();
        _freeCameraActor.SetActorLocationAndRotation(cameraLocation, cameraRotation, bSweep: false, out var _, bTeleport: true);
        _freeCameraActor.CallFunctionByNameWithArguments($"SetCameraFOV {_gameFov}", true);
        aBGPPlayerController.SetViewTargetWithBlend(_freeCameraActor);
        BGW_EventCollection.Get(world).Evt_SetInputMode(EGSInputMode.UIAndGame, EGSInputModeChangeReason.Replay);
        _isInFreeCameraMode = true;
        OnFreeCameraModeChanged?.Invoke(true);
    }

    public void LeaveFreeCameraMode()
    {
        var world = GameUtils.GetWorld();
        if (world == null)
        {
            return;
        }

        if (!_isInFreeCameraMode)
        {
            return;
        }

        var aBGPPlayerController = UGSE_EngineFuncLib.GetFirstLocalPlayerController(world) as ABGPPlayerController;
        if (aBGPPlayerController.IsNullOrDestroyed())
        {
            Logging.LogError("[FreeCameraManager] LeaveFreeCameraMode PlayerController IsNull");
            return;
        }

        if (_cacheCameraViewTarget.IsNullOrDestroyed())
        {
            var bGUCharacterCS = aBGPPlayerController.GetControlledPawn() as BGUCharacterCS;
            if (bGUCharacterCS.IsNullOrDestroyed())
            {
                Logging.LogError("[FreeCameraManager] LeaveFreeCameraMode PlayerCharacter IsNull");
                return;
            }

            aBGPPlayerController.SetViewTargetWithBlend(bGUCharacterCS);
        }
        else
        {
            aBGPPlayerController.SetViewTargetWithBlend(_cacheCameraViewTarget);
        }

        BGW_EnhancedInputMgrV2 bGW_EnhancedInputMgrV = BGW_EnhancedInputMgrV2.Get(world);
        if (bGW_EnhancedInputMgrV.InputModeTracker.InputMode == EGSInputMode.UIAndGame)
        {
            BGW_EventCollection.Get(world).Evt_SetInputMode(EGSInputMode.GameOnly, EGSInputModeChangeReason.Reset);
        }

        if (!_freeCameraActor.IsNullOrDestroyed())
        {
            BGU_UnrealWorldUtil.DestroyActor(_freeCameraActor);
        }

        Logging.LogInformation("[FreeCameraManager] Leaving free camera");
        _freeCameraActor = null;
        _cachePlayerPawn = null;
        _isInFreeCameraMode = false;
        OnFreeCameraModeChanged?.Invoke(false);
    }

    public void MoveFreeCameraToPosition(FVector position)
    {
        if (!IsInFreeCameraMode || _freeCameraActor.IsNullOrDestroyed())
        {
            return;
        }

        var currentLocation = _freeCameraActor.GetActorLocation();
        FVector moveOffset = position - currentLocation;
        MoveFreeCameraActor(moveOffset, isLocal: false);
    }

    public void MoveFreeCameraActor(FVector moveOffset, bool isLocal)
    {
        if (!IsInFreeCameraMode || _freeCameraActor.IsNullOrDestroyed())
        {
            return;
        }

        FVector actorLocation = _freeCameraActor.GetActorLocation();
        FVector currentMoveOffset = (isLocal ? _freeCameraActor.GetActorTransform().TransformVectorNoScale(moveOffset) : moveOffset);
        if (!MoveDetection(actorLocation, currentMoveOffset, out var adjustedMoveOffset, 0))
        {
            return;
        }

        _freeCameraActor.AddActorWorldOffset(adjustedMoveOffset, bSweep: true, out var sweepHitResult, bTeleport: false);
        if (sweepHitResult.BlockingHit)
        {
            FVector planeNormal = BGUFunctionLibraryCS.BGUGetVectorFromNetQuantizeVector(in sweepHitResult.Normal);
            FVector projectedMoveOffset = FVector.VectorPlaneProject(adjustedMoveOffset, planeNormal);
            if (MoveDetection(actorLocation, projectedMoveOffset, out var outputOffset, 1))
            {
                _freeCameraActor.AddActorWorldOffset(outputOffset, bSweep: true, out var _, bTeleport: false);
            }
        }
    }

    private bool MoveDetection(FVector currentCameraPos, FVector moveOffset, out FVector adjustedMoveOffset, int traceNum)
    {
        adjustedMoveOffset = moveOffset;
        FVector normalizedMoveOffset = moveOffset;
        normalizedMoveOffset.Normalize();
        USystemLibrary.SphereTraceSingle(GameUtils.GetWorld(), currentCameraPos + normalizedMoveOffset, currentCameraPos + moveOffset, 20f, ETraceTypeQuery.TraceTypeQuery2, bTraceComplex: false, [], EDrawDebugTrace.None, out var outHit, bIgnoreSelf: true, FLinearColor.Green, FLinearColor.Red, 1f);
        traceNum++;
        if (outHit.BlockingHit)
        {
            if (traceNum < 2)
            {
                FVector planeNormal = BGUFunctionLibraryCS.BGUGetVectorFromNetQuantizeVector(in outHit.Normal);
                FVector projectedMoveOffset = FVector.VectorPlaneProject(moveOffset, planeNormal);
                return MoveDetection(currentCameraPos, projectedMoveOffset, out adjustedMoveOffset, traceNum);
            }

            return false;
        }

        return true;
    }

    public void RotateFreeCameraActor(FRotator rotatorOffset, bool isLocal)
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            if (isLocal)
            {
                _freeCameraActor.AddActorLocalRotation(rotatorOffset, bSweep: true, out _, bTeleport: false);
            }
            else
            {
                _freeCameraActor.AddActorWorldRotation(rotatorOffset, bSweep: true, out _, bTeleport: false);
            }
        }
    }

    public void SetLookAtTarget(FVector targetLocation)
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            FVector actorLocation = _freeCameraActor.GetActorLocation();
            FRotator lookAtRotation = UMathLibrary.FindLookAtRotation(actorLocation, targetLocation);
            _freeCameraActor.SetActorRotation(lookAtRotation, false);
        }
    }
    
    public FVector GetCurrentCameraPosition()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
            return _freeCameraActor.GetActorLocation();
        return FVector.ZeroVector;
    }

    public FVector GetForwardVector()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            return _freeCameraActor.GetActorForwardVector();
        }
        return FVector.ForwardVector;
    }

    public FVector GetRightVector()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            return _freeCameraActor.GetActorRightVector();
        }
        return FVector.RightVector;
    }

    public float GetFreeCameraActorPitch()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            return _freeCameraActor.GetActorRotation().Pitch;
        }
        return 0f;
    }
}
