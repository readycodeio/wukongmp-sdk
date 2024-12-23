using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public static class Global
    {
        public static FVector CameraLookPosition { get; private set; }

        private static APlayerController _playerController;
        private static APlayerCameraManager _cameraManager;

        public static void TickWithGroup(float deltaTime)
        {
            if (_cameraManager == null)
            {
                _playerController = GameUtils.GetPlayerController();
                _cameraManager = _playerController.PlayerCameraManager;
            }

            // get main camera and find intersection with the ground
            var cameraLoc = _cameraManager.GetCameraLocation();
            var cameraRot = _cameraManager.GetCameraRotation();

            var traceEnd = cameraLoc + cameraRot.Vector() * 10000;
            var traceStart = cameraLoc;

            if (traceEnd.Z > traceStart.Z)
            {
                return;
            }

            // intersect with the plane at Z = playerLoc.Z, do not use Unreal raytrace
            var z = _playerController.GetActorLocation().Z;

            var hit = FMath.LinePlaneIntersection(traceStart, traceEnd, new FVector(0, 0, z), new FVector(0, 0, 1));
            CameraLookPosition = hit;
        }
    }
}