using b1;
using BtlShare;
using ReadyM.Relay.Client.State;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongPlayerModeManager(ClientState state, GameplayEventRouter eventRouter, FreeCameraManager freeCameraManager)
{
    private float _gravityScale = 0f;
    private FVector _lastValidLocation;

    public bool HandleBecameSpectator(PlayerEntity playerEntity, MainCharacterEntity mainEntity, bool isSpectator)
    {
        if (isSpectator)
            return HandleBecameSpectator(playerEntity, mainEntity);

        return HandleStoppedBeingSpectator(playerEntity, mainEntity);
    }

    public bool HandleBecameSpectator(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        var isMyself = mainComp.PlayerId == state.LocalPlayerId;

        if (isMyself)
        {
            UiUtils.SetHudVisibility(false);
        }

        SetPlayerVisibility(playerEntity, mainEntity, false);
        var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
        events?.Evt_BuffAllRemove.Invoke(EBuffEffectTriggerType.Remove);

        if (isMyself)
        {
            freeCameraManager.EnterFreeCameraMode();
            eventRouter.RaiseOnLocalPlayerChangedSpectator(true);
        }
        SetSpectatorCollisionEnabled(playerEntity, mainEntity, false);
        eventRouter.RaiseOnPlayerChangedTeam(playerEntity, mainEntity);
        
        return true;
    }

    public bool HandleStoppedBeingSpectator(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();

        var isMyself = mainComp.PlayerId == state.LocalPlayerId;

        if (isMyself)
            UiUtils.SetHudVisibility(true);

        SetPlayerVisibility(playerEntity, mainEntity, true);
        SetSpectatorCollisionEnabled(playerEntity, mainEntity, true);

        if (isMyself)
        {
            freeCameraManager.LeaveFreeCameraMode();
            eventRouter.RaiseOnLocalPlayerChangedSpectator(false);
        }
        eventRouter.RaiseOnPlayerChangedTeam(playerEntity, mainEntity);

        return true;
    }

    public bool SetPlayerVisibility(PlayerEntity playerEntity, MainCharacterEntity mainEntity, bool visible)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref var playerComp = ref playerEntity.GetState();

        var isVisible = localMainComp.Pawn?.Hidden == false;
        if (isVisible == visible)
            return false;

        Logging.LogDebug("Setting player {PlayerName} visibility to: {Visibility}", playerComp.NickName, visible);

        if (localMainComp.Pawn == null)
        {
            Logging.LogError("Player pawn is null");
            return false;
        }

        localMainComp.Pawn.SetActorHiddenInGame(!visible);
        localMainComp.MarkerActor?.SetActorHiddenInGame(!visible);

        var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantShowBlood, visible);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.IgnoreBattleInfoInUnitBar, visible);

        return true;
    }

    private bool SetSpectatorCollisionEnabled(PlayerEntity playerEntity, MainCharacterEntity mainEntity, bool enable)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref var playerComp = ref playerEntity.GetState();

        Logging.LogDebug("Setting player {PlayerName} collision to: {Enabled}", playerComp.NickName, enable);

        if (localMainComp.Pawn == null)
        {
            Logging.LogError("Player pawn is null");
            return false;
        }

        var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.ImmueDamage, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantBeBaseTarget, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantBeLock, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantBeAutoLockTarget, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.IgnoreAllInput, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantAttack, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.PELock, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.StaminaLock, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.PlayerCantLock, enable);

        if (enable)
        {
            localMainComp.Pawn.CharacterMovement.GravityScale = _gravityScale;
            PlayerUtils.TeleportLocalPlayer(mainEntity, _lastValidLocation, new FRotator(), false);
        }
        else
        {
            _gravityScale = localMainComp.Pawn.CharacterMovement.GravityScale;
            localMainComp.Pawn.CharacterMovement.GravityScale = 0;
            _lastValidLocation = localMainComp.Pawn.GetActorLocation();
            localMainComp.BeforeSpectatorLocation = _lastValidLocation;
            var offset = new FVector(0, 0, localMainComp.Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * -3);
            localMainComp.Pawn.SetActorLocation(_lastValidLocation + offset, false, out _, true);
        }
        localMainComp.Pawn.CharacterMovement.StopMovementImmediately();
        PlayerUtils.SetCollisionEnabled(localMainComp.Pawn, enable);
        return true;
    }
}