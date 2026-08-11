using System;
using System.Collections.Generic;
using System.Linq;
using B1UI;
using CSharpModBase;
using CSharpModBase.Input;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client.State;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.FreeCamera
{
    internal class FreeCameraController : IDisposable
    {
        private float _rotateDirLR;
        private float _rotateDirUD;
        private float _moveDirLR;
        private float _moveDirUD;
        private float _moveDirFB;

        private float _orbitYaw;
        private float _currentPlayerOrbitYaw;
        private FVector _targetLocation;
        private float _orbitPitch;
        private float _orbitDistance = 1000f;

        private const float MoveSpeed = 1000f;
        private const float RotateSpeed = 3000f;
        private const float OrbitDistanceMin = 200f;
        private const float OrbitDistanceMax = 2000f;
        private const float OrbitDistanceSpeed = 500f;
        private const float OrbitPitchSpeed = 60f;     // degrees per second
        private const float OrbitYawSpeed = 120f;      // degrees per second
        private const float OrbitYawFollowSpeed = 2f;
        private const float MouseOrbitSensitivity = 100f;

        private bool _isDragging;
        private FVector2D _lastDragPos;
        private MainCharacterEntity _spectatedEntity;

        private readonly FreeCameraManager _freeCameraManager;
        private readonly WukongPlayerState _playerState;
        private readonly ClientState _state;
        private readonly WukongWidgetManager _widgetManager;

        private (PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)? GetEntities(PlayerId playerId)
        {
            var playerEntity = _playerState.GetPlayerById(playerId);
            var mainEntity = _playerState.GetMainCharacterByPlayerId(playerId);
            if (!playerEntity.HasValue || !mainEntity.HasValue)
                return null;
            return (PlayerId: playerId, Player: playerEntity.Value, Character: mainEntity.Value);
        }

        private IEnumerable<PlayerId> AllNotSpectatingPlayerIds
            => _state.AreaPlayers.Where(p => _playerState.GetMainCharacterByPlayerId(p)?.GetState().IsSpectator == false);

        private IEnumerable<(PlayerId PlayerId, PlayerEntity Player, MainCharacterEntity Character)> AllPvPPlayers
            => AllNotSpectatingPlayerIds.Select(GetEntities).OfType<(PlayerId, PlayerEntity, MainCharacterEntity)>();

        private int _currentSpectatedIndex = -1;

        public FreeCameraController(
            ClientState state,
            WukongPlayerState playerState,
            InputManager inputManager,
            FreeCameraManager freeCameraManager,
            WukongWidgetManager widgetManager)
        {
            _state = state;
            _playerState = playerState;
            _freeCameraManager = freeCameraManager;
            _widgetManager = widgetManager;

            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.RBUTTON, OnRightMouseStarted, OnRightMouseCompleted));
            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.W, OnForwardStarted, OnForwardCompleted));
            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.S, OnBackwardStarted, OnBackwardCompleted));
            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.A, OnLeftStarted, OnLeftCompleted));
            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.D, OnRightStarted, OnRightCompleted));
            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.E, OnUpStarted, OnUpCompleted));
            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.Q, OnDownStarted, OnDownCompleted));

            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.RIGHT, OnNextStarted));
            inputManager.RegisterKeyBind(new HotKeyItem(ModifierKeys.None, Key.LEFT, OnPrevStarted));

            _freeCameraManager.OnFreeCameraModeChanged += OnFreeCameraModeChanged;
        }

        public void Dispose()
        {
            _freeCameraManager.OnFreeCameraModeChanged -= OnFreeCameraModeChanged;
        }

        public void Update(float DeltaTime)
        {
            ExecMove(DeltaTime);
            ExecRotate(DeltaTime);
        }

        private void ExecMove(float DeltaTime)
        {
            if (_currentSpectatedIndex == -1)
            {
                var upOffset = FVector.UpVector * _moveDirUD;
                var forwardOffset = _freeCameraManager.GetForwardVector() * _moveDirFB;
                var rightOffset = _freeCameraManager.GetRightVector() * _moveDirLR;
                var moveOffset = (upOffset + forwardOffset + rightOffset) * MoveSpeed * DeltaTime;
                if (moveOffset.Size() > 0f)
                {
                    _freeCameraManager.MoveFreeCameraActor(moveOffset, false);
                }
            }
        }

        private void ExecRotate(float DeltaTime)
        {
            CalculateMouseRotate();
            if (_currentSpectatedIndex == -1)
            {
                if (_rotateDirLR != 0f)
                {
                    FRotator rotatorOffsetLR = new FRotator(0.0, _rotateDirLR * RotateSpeed * DeltaTime, 0.0);
                    _freeCameraManager.RotateFreeCameraActor(rotatorOffsetLR, false);
                }
                if (_rotateDirUD != 0f)
                {
                    var rotatorPitch = _rotateDirUD * RotateSpeed * DeltaTime;
                    var updatedPitch = rotatorPitch + _freeCameraManager.GetFreeCameraActorPitch();
                    if (updatedPitch > 89f || updatedPitch < -89f)
                        return;
                    FRotator rotatorOffsetUD = new FRotator(rotatorPitch, 0.0, 0.0);
                    _freeCameraManager.RotateFreeCameraActor(rotatorOffsetUD, true);
                }
            }
            else
            {
                if (_spectatedEntity.IsNull)
                {
                    UpdateSpectatedPlayer(-1);
                    return;
                }
                if (_spectatedEntity.Pawn == null)
                {
                    UpdateSpectatedPlayer(-1);
                    return;
                }
                var spectatedCharacter = _spectatedEntity.Pawn;

                float orbitYawInput = -_moveDirLR + _rotateDirLR * MouseOrbitSensitivity;
                float orbitPitchInput = -_moveDirUD + _rotateDirUD * MouseOrbitSensitivity;

                _orbitYaw += orbitYawInput * OrbitYawSpeed * DeltaTime;
                _orbitPitch = FMath.Clamp(_orbitPitch + orbitPitchInput * OrbitPitchSpeed * DeltaTime, -89f, 89f);
                _orbitDistance = FMath.Clamp(_orbitDistance + -_moveDirFB * OrbitDistanceSpeed * DeltaTime, OrbitDistanceMin, OrbitDistanceMax);

                _targetLocation = spectatedCharacter.GetActorLocation();
                float playerYaw = spectatedCharacter.GetActorRotation().Yaw;

                _currentPlayerOrbitYaw = MathUtils.LerpAngle(_currentPlayerOrbitYaw, playerYaw, 1f - (float)Math.Exp(-OrbitYawFollowSpeed * DeltaTime));

                _freeCameraManager.SetFreeCameraActorTransform(_targetLocation, new(_orbitPitch, _currentPlayerOrbitYaw + _orbitYaw, 0.0f));
                _freeCameraManager.SetSpringArmLength(_orbitDistance);
            }
        }

        private void UpdateSpectatedPlayer(int direction)
        {
            var allPlayers = AllPvPPlayers.ToList();
            if (allPlayers.Count == 0)
            {
                DisablePlayerSpectating();
                return;
            }

            _currentSpectatedIndex += direction;
            if (_currentSpectatedIndex < 0)
            {
                DisablePlayerSpectating();
                return;
            }

            _currentSpectatedIndex = FMath.Clamp(_currentSpectatedIndex, 0, allPlayers.Count - 1);
            var spectatedPlayer = allPlayers[_currentSpectatedIndex];
            if (!spectatedPlayer.Character.HasUnsyncedPawn)
            {
                DisablePlayerSpectating();
                return;
            }

            _spectatedEntity = spectatedPlayer.Character;
            var spectatedCharacter = spectatedPlayer.Character.Pawn;
            var cameraPosition = _freeCameraManager.GetCurrentCameraPosition();
            var characterLocation = spectatedCharacter!.GetActorLocation();
            SetInitialOrbitFromCamera(cameraPosition, characterLocation, spectatedCharacter!.GetActorRotation());
            _widgetManager.SetSpectatingMessage(spectatedPlayer.Character.GetNickname().Nickname);
        }

        private void DisablePlayerSpectating()
        {
            // Reset position to spring arm position so that camera doesn't jump
            _freeCameraManager.SetFreeCameraActorTransform(_freeCameraManager.GetSpringArmEndTransform());
            _freeCameraManager.SetSpringArmLength(0);
            _currentSpectatedIndex = -1;
            _widgetManager.HideSpectatingMessage();
        }

        private void SetInitialOrbitFromCamera(FVector cameraPosition, FVector targetPosition, FRotator targetRotation)
        {
            FVector offset = cameraPosition - targetPosition;
            float distance = offset.Size();

            float playerYaw = targetRotation.Yaw;
            float offsetYaw = 180 + FMath.RadiansToDegrees(FMath.Atan2(offset.Y, offset.X));
            float relativeYaw = offsetYaw - playerYaw;
            relativeYaw = ((relativeYaw + 180f) % 360f) - 180f;

            float pitch = -FMath.RadiansToDegrees(FMath.Atan2(offset.Z, offset.Size2D()));

            _orbitYaw = relativeYaw;
            _orbitPitch = pitch;
            _orbitDistance = FMath.Clamp(distance, OrbitDistanceMin, OrbitDistanceMax);
            _currentPlayerOrbitYaw = playerYaw;
        }

        public void CalculateMouseRotate()
        {
            if (_isDragging)
            {
                FVector2D mouseScreenPosition = GSG.BattleLogicSvc.GetMouseScreenPosition();
                FVector2D dragRatio = GetDragRatio(mouseScreenPosition - _lastDragPos);
                _rotateDirLR = dragRatio.X;
                _rotateDirUD = (0f - dragRatio.Y);
                _lastDragPos = mouseScreenPosition;
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

        private void OnFreeCameraModeChanged(bool enabled)
        {
            if (!enabled)
            {
                DisablePlayerSpectating();
            }
            ResetInput();
        }

        private void ResetInput()
        {
            _isDragging = false;
            _rotateDirLR = 0f;
            _rotateDirUD = 0f;
            _moveDirLR = 0f;
            _moveDirUD = 0f;
            _moveDirFB = 0f;
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
                _rotateDirLR = 0f;
                _rotateDirUD = 0f;
            }
        }

        private void OnForwardStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _moveDirFB = 1f;
            }
        }

        private void OnForwardCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && _moveDirFB > 0f)
            {
                _moveDirFB = 0f;
            }
        }

        private void OnBackwardStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _moveDirFB = -1f;
            }
        }

        private void OnBackwardCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && _moveDirFB < 0f)
            {
                _moveDirFB = 0f;
            }
        }

        private void OnLeftStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _moveDirLR = -1f;
            }
        }

        private void OnLeftCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && _moveDirLR < 0f)
            {
                _moveDirLR = 0f;
            }
        }

        private void OnRightStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _moveDirLR = 1f;
            }
        }

        private void OnRightCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && _moveDirLR > 0f)
            {
                _moveDirLR = 0f;
            }
        }

        private void OnUpStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _moveDirUD = 1f;
            }
        }

        private void OnUpCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && _moveDirUD > 0f)
            {
                _moveDirUD = 0f;
            }
        }

        private void OnDownStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                _moveDirUD = -1f;
            }
        }

        private void OnDownCompleted()
        {
            if (_freeCameraManager.IsInFreeCameraMode && _moveDirUD < 0f)
            {
                _moveDirUD = 0f;
            }
        }

        private void OnNextStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                UpdateSpectatedPlayer(1);
            }
        }

        private void OnPrevStarted()
        {
            if (_freeCameraManager.IsInFreeCameraMode)
            {
                UpdateSpectatedPlayer(-1);
            }
        }
    }
}
