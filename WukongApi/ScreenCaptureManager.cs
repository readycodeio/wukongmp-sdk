using b1.BGW;
using b1;
using System.Collections.Generic;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using System.IO;

namespace WukongApi
{
    public class ScreenCaptureManager
    {
        private UTextureRenderTarget2D _screenRednerTarget;

        private AActor _screenCaptureActor;

        private bool _initialized = false;

        public void SetupScreenCapture()
        {
            var world = GameUtils.GetWorld();
            string renderTargetPath = "/Game/Mods/ScreenCaptureMod/RT_ScreenCapture.RT_ScreenCapture";
            _screenRednerTarget = BGW_PreloadAssetMgr.Get(world).RequestSyncLoadForUIResource<UTextureRenderTarget2D>(renderTargetPath, EUIResourceLoadType.CacheAndReleaseWhenChangeLevel);
            if (_screenRednerTarget == null)
            {
                Logging.LogError("Cannot load render target");
                return;
            }

            string screenActorPath = "/Game/Mods/ScreenCaptureMod/BP_ScreenCapture.BP_ScreenCapture_C";
            var screenActorClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(screenActorPath, ELoadResourceType.SyncLoadAndCache);
            if (screenActorClass == null)
            {
                Logging.LogError("Cannot load screen capture actor");
                return;
            }
            else
            {
                var player = GameUtils.GetBguPlayerCharacterCs();
                var camera = player.GetFollowCamera();
                var loc = camera.GetWorldLocation();
                var rot = camera.GetWorldRotation();
                _screenCaptureActor = world.SpawnActor(screenActorClass, ref loc, ref rot);

                if (_screenCaptureActor == null)
                {
                    Logging.LogDebug("Cannot spawn screen capture actor");
                    return;
                }

                Logging.LogDebug("Spawned screen capture actor");
                var localPlayerCameraManager = UGSE_EngineFuncLib.GetLocalPlayerCameraManager(world);
                var fov = localPlayerCameraManager.GetFOVAngle();
                _screenCaptureActor.CallFunctionByNameWithArguments($"SetFOV {fov}", true);
                _initialized = true;
            }
        }

        /// <summary>
        /// Saves render target data as .exr file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        public void SaveCapturedScreen(string filePath, string fileName)
        {
            if (EnsureInit())
            {
                URenderingLibrary.ExportRenderTarget(GameUtils.GetWorld(), _screenRednerTarget, filePath, Path.ChangeExtension(fileName, ".exr"));
            }
        }

        public List<FColor> GetCapturedScreenData()
        {
            if (EnsureInit())
            {
                URenderingLibrary.ReadRenderTarget(GameUtils.GetWorld(), _screenRednerTarget, out var outSamples);
                return outSamples;
            }
            return null;
        }

        private bool EnsureInit()
        {
            return _initialized;
        }
    }
}
