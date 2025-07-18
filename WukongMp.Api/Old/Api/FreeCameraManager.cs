using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old.Api
{
    public class FreeCameraManager
    {
        private bool _isInFreeCameraMode;
        private BGUCharacterCS? _cachePlayerPawn;
        private AActor? _freeCameraActor;
        private float _gameFov;
        private AActor? _cacheCameraViewTarget;
        private const string FreeCameraActorPath = "/Game/Mods/CustomLuaMod/BP_FreeCameraActor.BP_FreeCameraActor_C";
        
        public static FreeCameraManager Instance { get; } = new();
        private FreeCameraManager() {}

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
            FreeCameraControlsWidget.Instance.SetVisibility(true);
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
            FreeCameraControlsWidget.Instance.SetVisibility(false);
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