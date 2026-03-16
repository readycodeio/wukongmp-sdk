using System;
using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.FreeCamera;

internal sealed class FreeCameraManager
{
    private const string FreeCameraActorPath = "/Game/Mods/WukongMod/BP_FreeCameraActor.BP_FreeCameraActor_C";

    private BGUCharacterCS? _cachePlayerPawn;
    private AActor? _freeCameraActor;
    private USpringArmComponent? _springArmComponent;
    private float _gameFov;
    private AActor? _cacheCameraViewTarget;
    private readonly WukongPlayerState playerState;
    
    internal FreeCameraManager(WukongPlayerState playerState)
    {
        this.playerState = playerState;
    }
    
    public bool IsInFreeCameraMode { get; private set; }

    public event Action<bool>? OnFreeCameraModeChanged;

    public void EnterFreeCameraMode()
    {
        var world = GameUtils.GetWorld();
        if (world == null)
        {
            return;
        }

        if (IsInFreeCameraMode)
        {
            return;
        }

        _cachePlayerPawn = playerState.LocalMainCharacter?.Pawn;
        if (_cachePlayerPawn.IsNullOrDestroyed())
        {
            Logging.LogError("[FreeCameraManager] EnterFreeCameraMode PlayerPawn IsNull");
            return;
        }

        var aBGPPlayerController = _cachePlayerPawn.GetController() as ABGPPlayerController;
        if (aBGPPlayerController.IsNullOrDestroyed())
        {
            Logging.LogError("[FreeCameraManager] EnterFreeCameraMode PlayerController IsNull");
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

        _springArmComponent = _freeCameraActor.GetComponentByClass<USpringArmComponent>();
        if (_springArmComponent == null)
        {
            Logging.LogError("[FreeCameraManager] FreeCameraActor SpringArmComponent IsNull");
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
        IsInFreeCameraMode = true;
        OnFreeCameraModeChanged?.Invoke(true);
    }

    public void LeaveFreeCameraMode()
    {
        var world = GameUtils.GetWorld();
        if (world == null)
        {
            return;
        }

        if (!IsInFreeCameraMode)
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
        _springArmComponent = null;
        _cachePlayerPawn = null;
        IsInFreeCameraMode = false;
        OnFreeCameraModeChanged?.Invoke(false);
    }

    internal void ReEnableFreeCamera()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed() && _cachePlayerPawn != null)
        {
            var aBGPPlayerController = _cachePlayerPawn.GetController() as ABGPPlayerController;
            if (aBGPPlayerController.IsNullOrDestroyed())
            {
                Logging.LogError("[FreeCameraManager] EnterFreeCameraMode PlayerController IsNull");
                return;
            }
            _cacheCameraViewTarget = aBGPPlayerController.GetViewTarget();
            aBGPPlayerController.SetViewTargetWithBlend(_freeCameraActor);
        }
    }

    internal bool MoveFreeCameraToPosition(FVector position)
    {
        if (!IsInFreeCameraMode || _freeCameraActor.IsNullOrDestroyed())
        {
            return false;
        }

        var currentLocation = _freeCameraActor.GetActorLocation();
        FVector moveOffset = position - currentLocation;
        return MoveFreeCameraActor(moveOffset, isLocal: false);
    }

    internal bool MoveFreeCameraWithObstacleCheck(FVector targetPosition, FVector desiredCameraPosition, float safeDistance = 20f)
    {
        if (!IsInFreeCameraMode || _freeCameraActor.IsNullOrDestroyed())
        {
            return false;
        }

        var world = GameUtils.GetWorld();
        if (world == null)
        {
            return false;
        }

        USystemLibrary.SphereTraceSingle(world, targetPosition, desiredCameraPosition, safeDistance, ETraceTypeQuery.TraceTypeQuery2, bTraceComplex: false, [], EDrawDebugTrace.None, out var hitResult, bIgnoreSelf: true, FLinearColor.Green, FLinearColor.Red, 1f);

        FVector finalPosition = desiredCameraPosition;
        if (hitResult.BlockingHit)
        {
            finalPosition = BGUFunctionLibraryCS.BGUGetVectorFromNetQuantizeVector(hitResult.Location);
        }
        _freeCameraActor.SetActorLocation(finalPosition, false, out _, true);
        UpdatePawnPositionToCamera();
        return true;
    }

    internal bool MoveFreeCameraActor(FVector moveOffset, bool isLocal)
    {
        if (!IsInFreeCameraMode || _freeCameraActor.IsNullOrDestroyed())
        {
            return false;
        }

        FVector actorLocation = _freeCameraActor.GetActorLocation();
        FVector currentMoveOffset = (isLocal ? _freeCameraActor.GetActorTransform().TransformVectorNoScale(moveOffset) : moveOffset);
        if (!MoveDetection(actorLocation, currentMoveOffset, out var adjustedMoveOffset, 0))
        {
            return false;
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
        UpdatePawnPositionToCamera();
        return true;
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

    internal void RotateFreeCameraActor(FRotator rotatorOffset, bool isLocal)
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

    internal void SetLookAtTarget(FVector targetLocation)
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            FVector actorLocation = _freeCameraActor.GetActorLocation();
            FRotator lookAtRotation = UMathLibrary.FindLookAtRotation(actorLocation, targetLocation);
            _freeCameraActor.SetActorRotation(lookAtRotation, false);
        }
    }
    
    internal FVector GetCurrentCameraPosition()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
            return GetSpringArmEndTransform().GetLocation();
        return FVector.ZeroVector;
    }

    internal FVector GetForwardVector()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            return _freeCameraActor.GetActorForwardVector();
        }
        return FVector.ForwardVector;
    }

    internal FVector GetRightVector()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            return _freeCameraActor.GetActorRightVector();
        }
        return FVector.RightVector;
    }

    internal float GetFreeCameraActorPitch()
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            return _freeCameraActor.GetActorRotation().Pitch;
        }
        return 0f;
    }

    internal void SetFreeCameraActorTransform(FVector location, FRotator rotation)
    {
        if (IsInFreeCameraMode && !_freeCameraActor.IsNullOrDestroyed())
        {
            _freeCameraActor.SetActorLocationAndRotation(location, rotation, bSweep: false, out var _, bTeleport: true);
            UpdatePawnPositionToCamera();
        }
    }

    internal void SetFreeCameraActorTransform(FTransform transform)
    {
        SetFreeCameraActorTransform(transform.GetLocation(), transform.GetRotation().Rotator());
    }

    internal void SetSpringArmLength(float length)
    {
        if (IsInFreeCameraMode && !_springArmComponent.IsNullOrDestroyed())
        {
            _springArmComponent.TargetArmLength = length;
            UpdatePawnPositionToCamera();
        }
    }

    internal FTransform GetSpringArmEndTransform()
    {
        if (IsInFreeCameraMode && !_springArmComponent.IsNullOrDestroyed())
        {
            return _springArmComponent.GetSocketTransform(new FName(Constants.SpringArmEndSocket));
        }
        return FTransform.Identity;
    }

    /// <summary>
    /// Updates the pawn's position to align with the camera's current location in order to load level where the camera is.
    /// </summary>
    private void UpdatePawnPositionToCamera()
    {
        if (IsInFreeCameraMode && !_cachePlayerPawn.IsNullOrDestroyed())
        {
            _cachePlayerPawn.SetActorLocation(GetCurrentCameraPosition(), false, out _, true);
        }
    }
}
