using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public class FreeCameraManager
    {
        private bool _isInFreeCameraMode;
        private BGUCharacterCS _cachePlayerPawn;
        private AActor _freeCameraActor;
        private float _gameFOV;
        private AActor _cacheCameraViewTarget;
        private const string _freeCameraActorPath = "/Game/Mods/CustomLuaMod/BP_FreeCameraActor.BP_FreeCameraActor_C";

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

            ABGPPlayerController aBGPPlayerController = UGSE_EngineFuncLib.GetFirstLocalPlayerController(world) as ABGPPlayerController;
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

            APlayerCameraManager localPlayerCameraManager = UGSE_EngineFuncLib.GetLocalPlayerCameraManager(world);
            if (localPlayerCameraManager.IsNullOrDestroyed())
            {
                Logging.LogError("[FreeCameraManager] EnterFreeCameraMode PlayerCameraManager IsNull");
                return;
            }

            FVector cameraLocation = localPlayerCameraManager.GetCameraLocation();
            FRotator cameraRotation = localPlayerCameraManager.GetCameraRotation();
            if (_freeCameraActor.IsNullOrDestroyed())
            {
                var freeCameraActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(_freeCameraActorPath, ELoadResourceType.SyncLoadAndCache);
                _freeCameraActor = world.SpawnActor(freeCameraActorClass, ref cameraLocation, ref cameraRotation);
            }

            if (_freeCameraActor.IsNullOrDestroyed())
            {
                Logging.LogError("[FreeCameraManager] EnterFreeCameraMode Spawn FreeCameraActor Failed");
                return;
            }

            _freeCameraActor.SetActorHiddenInGame(bNewHidden: false);
            _freeCameraActor.SetActorEnableCollision(bNewActorEnableCollision: true);
            _cacheCameraViewTarget = aBGPPlayerController.GetViewTarget();
            _gameFOV = localPlayerCameraManager.GetFOVAngle();
            _freeCameraActor.SetActorLocationAndRotation(cameraLocation, cameraRotation, bSweep: false, out var _, bTeleport: true);
            _freeCameraActor.CallFunctionByNameWithArguments($"SetCameraFOV {_gameFOV}", true);
            aBGPPlayerController.SetViewTargetWithBlend(_freeCameraActor);
            _cachePlayerPawn.DisableInput(aBGPPlayerController);
            BGW_EventCollection.Get(world).Evt_SetInputMode(EGSInputMode.Replay, EGSInputModeChangeReason.Replay);
            _isInFreeCameraMode = true;
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

            ABGPPlayerController aBGPPlayerController = UGSE_EngineFuncLib.GetFirstLocalPlayerController(world) as ABGPPlayerController;
            if (aBGPPlayerController.IsNullOrDestroyed())
            {
                Logging.LogError("[FreeCameraManager] LeaveFreeCameraMode PlayerController IsNull");
                return;
            }

            if (_cacheCameraViewTarget.IsNullOrDestroyed())
            {
                BGUCharacterCS bGUCharacterCS = aBGPPlayerController.GetControlledPawn() as BGUCharacterCS;
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

            _cachePlayerPawn.EnableInput(aBGPPlayerController);
            BGW_EventCollection.Get(world).Evt_SetInputMode(EGSInputMode.GameOnly, EGSInputModeChangeReason.Reset);

            if (!_freeCameraActor.IsNullOrDestroyed())
            {
                BGU_UnrealWorldUtil.DestroyActor(_freeCameraActor);
            }

            _freeCameraActor = null;
            _cachePlayerPawn = null;
            _isInFreeCameraMode = false;
        }

        public void SwitchFreeCameraMode()
        {
            if (_isInFreeCameraMode)
            {
                LeaveFreeCameraMode();
            }
            else
            {
                EnterFreeCameraMode();
            }
        }
    }
}