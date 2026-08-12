using b1;
using BtlShare;
using ReadyM.Relay.Client.State;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

internal class WukongPlayerModeManager(ClientState state, GameplayEventRouter eventRouter, FreeCameraManager freeCameraManager)
{
    private float _gravityScale;
    private FVector _lastValidLocation;

    public bool HandleBecameSpectator(MainCharacterEntity mainEntity, bool isSpectator)
    {
        if (isSpectator)
            return HandleBecameSpectator(mainEntity);

        return HandleStoppedBeingSpectator(mainEntity);
    }

    public bool HandleBecameSpectator(MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        var isMyself = mainComp.PlayerId == state.LocalPlayerId;

        if (isMyself)
        {
            UiUtils.SetHudVisibility(false);
        }

        SetPlayerVisibility(mainEntity, false);
        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_BuffAllRemove.Invoke(EBuffEffectTriggerType.Remove);

        if (isMyself)
        {
            freeCameraManager.EnterFreeCameraMode();
            eventRouter.RaiseOnLocalPlayerChangedSpectator(true);
        }

        SetSpectatorCollisionEnabled(mainEntity, false);

        var playerId = mainComp.PlayerId;
        if (state.PlayerEntries.TryGetValue(playerId, out var playerEntry))
        {
            var playerEntity = new PlayerEntity(playerEntry.PlayerEntity);
            eventRouter.RaiseOnPlayerChangedTeam(playerEntity, mainEntity);
        }

        return true;
    }

    public bool HandleStoppedBeingSpectator(MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();

        var isMyself = mainComp.PlayerId == state.LocalPlayerId;

        if (isMyself)
            UiUtils.SetHudVisibility(true);

        SetPlayerVisibility(mainEntity, true);
        SetSpectatorCollisionEnabled(mainEntity, true);

        if (isMyself)
        {
            freeCameraManager.LeaveFreeCameraMode();
            eventRouter.RaiseOnLocalPlayerChangedSpectator(false);
        }

        var playerId = mainComp.PlayerId;
        if (state.PlayerEntries.TryGetValue(playerId, out var playerEntry))
        {
            var playerEntity = new PlayerEntity(playerEntry.PlayerEntity);
            eventRouter.RaiseOnPlayerChangedTeam(playerEntity, mainEntity);
        }

        return true;
    }

    public bool SetPlayerVisibility(MainCharacterEntity mainEntity, bool visible)
    {
        var isVisible = mainEntity.Pawn?.Hidden == false;
        if (isVisible == visible)
            return false;

        Logging.LogDebug("Setting character {CharacterNickName} visibility to: {Visibility}", mainEntity.GetNickname().Nickname, visible);

        if (mainEntity.Pawn == null)
        {
            Logging.LogError("Player pawn is null");
            return false;
        }

        mainEntity.Pawn.SetActorHiddenInGame(!visible);
        mainEntity.GetMarker().MarkerActor?.SetActorHiddenInGame(!visible);

        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantShowBlood, IsRemove: visible);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.IgnoreBattleInfoInUnitBar, IsRemove: visible);

        return true;
    }

    private bool SetSpectatorCollisionEnabled(MainCharacterEntity mainEntity, bool enable)
    {
        Logging.LogDebug("Setting player {PlayerName} collision to: {Enabled}", mainEntity.GetNickname().Nickname, enable);

        if (mainEntity.Pawn == null)
        {
            Logging.LogError("Player pawn is null");
            return false;
        }

        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.ImmueDamage, IsRemove: enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantBeBaseTarget, IsRemove: enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantBeLock, IsRemove: enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantBeAutoLockTarget, IsRemove: enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.IgnoreAllInput, IsRemove: enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.PELock, IsRemove: enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.StaminaLock, IsRemove: enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.PlayerCantLock, IsRemove: enable);

        if (enable)
        {
            _lastValidLocation = mainEntity.Pawn.GetActorLocation(); // TODO: Only in co-op

            mainEntity.Pawn.CharacterMovement.GravityScale = _gravityScale;
            PlayerUtils.TeleportLocalPlayer(mainEntity, _lastValidLocation, new FRotator(), false);
        }
        else
        {
            _gravityScale = mainEntity.Pawn.CharacterMovement.GravityScale;
            mainEntity.Pawn.CharacterMovement.GravityScale = 0;
            _lastValidLocation = mainEntity.Pawn.GetActorLocation();
            var offset = new FVector(0, 0, mainEntity.Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * -3);
            mainEntity.Pawn.SetActorLocation(_lastValidLocation + offset, false, out _, true);
        }

        mainEntity.Pawn.CharacterMovement.StopMovementImmediately();
        PlayerUtils.SetCollisionEnabled(mainEntity.Pawn, enable);
        return true;
    }
}