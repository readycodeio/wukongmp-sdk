using b1;
using BtlShare;
using System.Globalization;
using ReadyM.Api.Command;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.Command;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP.Command;

public class PvpCommandRegistration(
    WukongPlayerState playerState,
    WukongAreaState areaState,
    WukongClientRpcCallbacks clientRpc,
    WukongChatter chatter,
    WukongCommandConsole console
) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("spawn", ConsoleCommand.Create(RequestSpawn, false), UnitPathUtils.GetAllValidUnitNames());
        registry.AddCommand("spectator", ConsoleCommand.Create(SetSpectatorStatus, false));
        registry.AddCommand("instant_cooldown", ConsoleCommand.Create(ToggleSkillsCooldown, false));
        registry.AddCommand("infinite_mana", ConsoleCommand.Create(ToggleInfiniteMana, false));
        registry.AddCommand("spirit_cooldown", ConsoleCommand.Create(SetSpiritCooldown, false));
        registry.AddCommand("infinite_vessel", ConsoleCommand.Create(ToggleInfiniteVessel, false));
        registry.AddCommand("infinite_transform", ConsoleCommand.Create(ToggleInfiniteTransform, false));
        registry.AddCommand("arena", ConsoleCommand.Create(TeleportToArena, false));
        registry.AddCommand("shrine", ConsoleCommand.Create(TeleportToShrine, false));

        registry.AddCommand("pvp_level", ConsoleCommand.Create(TeleportToPvpLevel, true));
    }

    private void RequestSpawn(string unitName, int count = 1)
    {
        {
            console.AddMessage(string.Format(Texts.InvalidUnitName, unitName));
            return;
        }

        var playerEntity = playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;

        var characterEntity = playerState.LocalMainCharacter;
        if (characterEntity == null)
            return;

        var teamId = PvpUtils.GetOppositeTeam(playerEntity.Value.GetState().TeamId);
        var playerPawn = characterEntity.Value.Pawn;
        if (playerPawn == null)
            return;

        var location = SpawningUtils.CalculateSpawnLocation(playerPawn.GetActorLocation(), playerPawn.GetActorForwardVector());

        clientRpc.SendRequestSpawnUnits(new RequestSpawnUnitsData(unitName, count, teamId, location));
        chatter.SendServerMessage("PlayerSpawned", characterEntity.Value.GetState().CharacterNickName, count.ToString(), unitName);
    }

    private void SetSpectatorStatus()
    {
        var playerEntity = playerState.LocalMainCharacter;
        if (playerEntity == null)
            return;

        if (!areaState.PvpState!.Value.InTournament)
        {
            ref var pvp = ref playerEntity.Value.GetPvP();
            if (!pvp.IsSpectator)
            {
                PlayerUtils.EnableSpectator(playerEntity.Value, SpectatorReason.Observer);
            }
            else
            {
                PlayerUtils.DisableSpectator(playerEntity.Value);
            }
        }
    }

    private void ToggleInfiniteMana()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea.HasValue && !areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            console.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        ref var localStateComp = ref mainEntity.GetLocalState();
        if (mainEntity.Pawn != null)
        {
            PlayerUtils.ResetMana(mainEntity.Pawn);
        }

        localStateComp.HasInfiniteMana = !localStateComp.HasInfiniteMana;
        chatter.SendServerMessage(localStateComp.HasInfiniteMana ? "InfManaEnabled" : "InfManaDisabled", playerState.NickName);
    }

    private void SetSpiritCooldown(float spiritCooldownTime)
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea.HasValue && !areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            console.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        if (spiritCooldownTime < 0)
        {
            console.AddLocalizedMessage("InvalidCooldown");
            return;
        }

        ref var localStateComp = ref mainEntity.GetLocalState();
        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            localStateComp.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.VigorEnergyMax));
            localStateComp.ShouldSetSpiritCooldown = false;
        }

        localStateComp.SpiritCooldownEnabled = true;
        localStateComp.SpiritCooldownTime = spiritCooldownTime;
        chatter.SendServerMessage("CustomSpiritCooldown", playerState.NickName, spiritCooldownTime.ToString(CultureInfo.InvariantCulture));
    }

    private void ToggleInfiniteVessel()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea is { Room.CheatsAllowed: false })
        {
            console.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.FabaoEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.FabaoEnergyMax));
        }

        mainEntity.GetLocalState().HasInfiniteVessel = !mainEntity.GetLocalState().HasInfiniteVessel;
        chatter.SendServerMessage(mainEntity.GetLocalState().HasInfiniteVessel ? "InfVesselEnabled" : "InfVesselDisabled", playerState.NickName);
    }

    private void ToggleInfiniteTransform()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea is { Room.CheatsAllowed: false })
        {
            console.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        if (mainEntity.HasPawn)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.CurEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.TransEnergyMax));
        }

        mainEntity.GetLocalState().HasInfiniteTransform = !mainEntity.GetLocalState().HasInfiniteTransform;
        var playerComp = mainEntity.GetState();
        chatter.SendServerMessage(mainEntity.GetLocalState().HasInfiniteTransform ? "InfTransformEnabled" : "InfTransformDisabled", playerComp.CharacterNickName);
    }

    private void ToggleSkillsCooldown()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea.HasValue && !areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            console.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        ref var localStateComp = ref mainEntity.GetLocalState();
        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_ResetSkillCD.Invoke();
        localStateComp.InstantSkillCooldown = !localStateComp.InstantSkillCooldown;
        chatter.SendServerMessage(mainEntity.GetLocalState().InstantSkillCooldown ? "InstantCooldownEnabled" : "InstantCooldownDisabled", playerState.NickName);
    }

    private void TeleportToArena()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.InRoom && !mainEntity.GetPvP().IsSpectator && areaState.PvpState is { InTournament: false })
        {
            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            PlayerUtils.TeleportLocalPlayer(mainEntity, levelData.PvpStartingLocation, FRotator.ZeroRotator);
        }
    }

    private void TeleportToShrine()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.InRoom && !mainEntity.GetPvP().IsSpectator && areaState.PvpState is { InTournament: false })
        {
            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            PlayerUtils.TeleportLocalPlayerToRebirthPoint(mainEntity, levelData.BirthPointID);
        }
    }

    private void TeleportToPvpLevel(int pvpLevelId)
    {
        if (playerState.LocalMainCharacter is not { } mainEntity || !areaState.InRoom || mainEntity.GetPvP().IsSpectator || areaState.PvpState is { InTournament: true })
            return;

        if (pvpLevelId < 0)
        {
            console.AddMessage(Texts.InvalidCommand);
            return;
        }

        LaunchParameters.Instance.LevelId = pvpLevelId;
        var levelData = LevelSpawnConfig.GetLevelSpawnData(pvpLevelId);
        BPS_EventCollectionCS.GetLocal(GameUtils.GetWorld()).Evt_BPS_TeleportTo.Invoke(ETeleportTypeV2.RebirthPointTeleportOnly, new TeleportParam_RebirthPoint
        {
            RebirthPointId = levelData.BirthPointID,
        }, EPlayerTeleportReason.RebirthPoint);
    }
}