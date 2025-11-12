using b1;
using BtlShare;
using ReadyM.Relay.Client.State;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongPlayerModeManager(ClientState state, WukongAreaState areaState, WukongWidgetManager widgetManager)
{
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
            FreeCameraManager.Instance.EnterFreeCameraMode();
            PvPUtils.SetupSpectatorUi();
        }
        SetPlayerCollision(playerEntity, mainEntity, false);

        widgetManager.UpdatePlayerTeam(playerEntity, mainEntity);
        return true;
    }

    public bool HandleStoppedBeingSpectator(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();

        var isMyself = mainComp.PlayerId == state.LocalPlayerId;

        if (isMyself)
            UiUtils.SetHudVisibility(true);

        SetPlayerVisibility(playerEntity, mainEntity, true);
        SetPlayerCollision(playerEntity, mainEntity, true);

        if (isMyself)
        {
            FreeCameraManager.Instance.LeaveFreeCameraMode();

            if (areaState.PvpState is not { InPvP: true })
            {
                PvPUtils.SetupLobbyUi();
            }
            else
            {
                LobbyStatusWidget.Instance.SetVisibility(false);
                CoopStatusWidget.Instance.SetVisibility(false);
            }
        }

        widgetManager.UpdatePlayerTeam(playerEntity, mainEntity);

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
        return true;
    }

    private bool SetPlayerCollision(PlayerEntity playerEntity, MainCharacterEntity mainEntity, bool enable)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref var playerComp = ref playerEntity.GetState();

        Logging.LogDebug("Setting player {PlayerName} collision to: {Enabled}", playerComp.NickName, enable);

        if (localMainComp.Pawn == null)
        {
            Logging.LogError("Player pawn is null");
            return false;
        }

        var offset = new FVector(0,0, localMainComp.Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * 3 * (enable ? 1 : -1));
        localMainComp.Pawn.SetActorLocation(localMainComp.Pawn.GetActorLocation() + offset, false, out _, true);
        var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.ImmueDamage, enable);
        events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantBeBaseTarget, enable);
        PlayerUtils.EnablePlayerPawnCollision(localMainComp.Pawn, enable);
        return true;
    }

    public void UpdatePlayerTeam(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        ref var playerComp = ref playerEntity.GetState();
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref readonly var teamComp = ref mainEntity.GetTeam();

        Logging.LogDebug("Updating player {Nickname} to team {Team}", playerComp.NickName, teamComp.TeamId);

        var pawn = localMainComp.Pawn;

        if (pawn == null)
            return;

        ClientUtils.RegisterAndSetPlayerTeam(pawn, teamComp.TeamId);

        if (localMainComp.MarkerActor != null)
        {
            var teamColor = Constants.IsCoop ? MarkerUtils.WhiteTeamColor : PvPUtils.GetTeamColorString(teamComp.TeamId);
            localMainComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {mainComp.CharacterNickName} {teamColor}", true);
        }

        widgetManager.UpdatePlayerTeam(playerEntity, mainEntity);
    }
}