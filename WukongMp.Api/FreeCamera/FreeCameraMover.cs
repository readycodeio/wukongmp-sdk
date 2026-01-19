using B1UI;
using CSharpModBase;
using CSharpModBase.Input;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.FreeCamera
{
    public class FreeCameraMover
    {
        private bool _isDragging;
        private FVector2D _lastDragPos;
        private readonly FreeCameraManager _freeCameraManager;

        public float RotateDirLR { get; private set; }
        public float RotateDirUD { get; private set; }
        public float MoveDirLR { get; private set; }
        public float MoveDirUD { get; private set; }
        public float MoveDirFB { get; private set; }
        public float RotateSpeed { get; private set; }
        public float Speed { get; private set; }

        public FreeCameraMover(InputManager inputManager, FreeCameraManager freeCameraManager)
        {
            _freeCameraManager = freeCameraManager;

            HotKeyItem rightMouseAction = new(ModifierKeys.None, Key.RBUTTON, OnRightMouseStarted, OnRightMouseCompleted);
            HotKeyItem forwardAction = new(ModifierKeys.None, Key.W, OnForwardStarted, OnForwardCompleted);
            HotKeyItem backwardAction = new(ModifierKeys.None, Key.S, OnBackwardStarted, OnBackwardCompleted);
            HotKeyItem leftAction = new(ModifierKeys.None, Key.A, OnLeftStarted, OnLeftCompleted);
            HotKeyItem rightAction = new(ModifierKeys.None, Key.D, OnRightStarted, OnRightCompleted);
            HotKeyItem upAction = new(ModifierKeys.None, Key.E, OnUpStarted, OnUpCompleted);
            HotKeyItem downAction = new(ModifierKeys.None, Key.Q, OnDownStarted, OnDownCompleted);

            inputManager.RegisterKeyBind(rightMouseAction);
            inputManager.RegisterKeyBind(forwardAction);
            inputManager.RegisterKeyBind(backwardAction);
            inputManager.RegisterKeyBind(leftAction);
            inputManager.RegisterKeyBind(rightAction);
            inputManager.RegisterKeyBind(upAction);
            inputManager.RegisterKeyBind(downAction);

            Speed = Constants.FreeCameraMoveSpeed;
            RotateSpeed = Constants.FreeCameraRotateSpeed;
        }

        public void Update(float DeltaTime)
        {
            CalculateMouseRotate();
            ExecMove(DeltaTime);
            ExecRotate(DeltaTime);
        }

        private void ExecMove(float DeltaTime)
        {
            var upOffset = FVector.UpVector * MoveDirUD;
            var forwardOffset = _freeCameraManager.GetForwardVector() * MoveDirFB;
            var rightOffset = _freeCameraManager.GetRightVector() * MoveDirLR;
            var moveOffset = (upOffset + forwardOffset + rightOffset) * Speed * DeltaTime;
            if (moveOffset.Size() > 0f)
            {
                _freeCameraManager.MoveFreeCameraActor(moveOffset, false);
            }
        }

        private void ExecRotate(float DeltaTime)
        {
            if (RotateDirLR != 0f)
            {
                FRotator rotatorOffsetLR = new FRotator(0.0, RotateDirLR * RotateSpeed * DeltaTime, 0.0);
                _freeCameraManager.RotateFreeCameraActor(rotatorOffsetLR, false);
            }
            if (RotateDirUD != 0f)
            {
                var rotatorPitch = RotateDirUD * RotateSpeed * DeltaTime;
                var updatedPitch = rotatorPitch + _freeCameraManager.GetFreeCameraActorPitch();
                if (updatedPitch > 89f || updatedPitch < -89f)
                    return;
                FRotator rotatorOffsetUD = new FRotator(rotatorPitch, 0.0, 0.0);
                _freeCameraManager.RotateFreeCameraActor(rotatorOffsetUD, true);
            }
        }

        private void OnRightMouseStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _isDragging = true;
                _lastDragPos = GSG.BattleLogicSvc.GetMouseScreenPosition();
            }
        }

        private void OnRightMouseCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _isDragging = false;
                RotateDirLR = 0f;
                RotateDirUD = 0f;
            }
        }

        public void CalculateMouseRotate()
        {
            if (_isDragging)
            {
                FVector2D mouseScreenPosition = GSG.BattleLogicSvc.GetMouseScreenPosition();
                FVector2D dragRatio = GetDragRatio(mouseScreenPosition - _lastDragPos);
                RotateDirLR = dragRatio.X;
                RotateDirUD = (0f - dragRatio.Y);
                _lastDragPos = mouseScreenPosition;
            }
        }
        private void OnForwardStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                MoveDirFB = 1f;
            }
        }

        private void OnForwardCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && MoveDirFB > 0f)
            {
                MoveDirFB = 0f;
            }
        }

        private void OnBackwardStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                MoveDirFB = -1f;
            }
        }

        private void OnBackwardCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && MoveDirFB < 0f)
            {
                MoveDirFB = 0f;
            }
        }

        private void OnLeftStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                MoveDirLR = -1f;
            }
        }

        private void OnLeftCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && MoveDirLR < 0f)
            {
                MoveDirLR = 0f;
            }
        }

        private void OnRightStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                MoveDirLR = 1f;
            }
        }

        private void OnRightCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && MoveDirLR > 0f)
            {
                MoveDirLR = 0f;
            }
        }

        private void OnUpStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                MoveDirUD = 1f;
            }
        }

        private void OnUpCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && MoveDirUD > 0f)
            {
                MoveDirUD = 0f;
            }
        }

        private void OnDownStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                MoveDirUD = -1f;
            }
        }

        private void OnDownCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && MoveDirUD < 0f)
            {
                MoveDirUD = 0f;
            }
        }

        private FVector2D GetDragRatio(FVector2D mouseOffset)
        {
            var world = GameUtils.GetWorld();
            float viewPortScale = UWidgetLayoutLibrary.GetViewportScale(world);
            FVector2D viewportSize = UWidgetLayoutLibrary.GetViewportSize(world) * viewPortScale;
            float normalizedOffsetX = 0f;
            float normalizedOffsetY = 0f;
            if (viewportSize.X > 0f)
            {
                normalizedOffsetX = mouseOffset.X / viewportSize.X;
            }
            if (viewportSize.Y > 0f)
            {
                normalizedOffsetY = mouseOffset.Y / viewportSize.Y;
            }
            return new FVector2D(normalizedOffsetX, normalizedOffsetY);
        }
    }
}
