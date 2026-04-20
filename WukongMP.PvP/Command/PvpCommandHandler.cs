using System.Globalization;
using System.Linq;
using b1;
using BtlShare;
using ReadyM.Api.Command;
using ReadyM.Api.DI;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Resources;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.Command;

public class PvpCommandHandler(
    IWukongConsoleApi consoleApi,
    IWukongChatApi chatApi,
    IWukongPvpApi pvpApi,
    IWukongCheatsApi cheatsApi,
    IWukongSynchronizationApi syncApi
) : IHostedService
{
    public void OnScopeStart()
    {
        var allmonsterNames = TamerKinds.GetAllValidTamerKinds().Select(x => x.Name);
        consoleApi.AddCommand("spawn", ConsoleCommand.Create(RequestSpawn, false), allmonsterNames);

        consoleApi.AddCommand("spectator", ConsoleCommand.Create(SetSpectatorStatus, false));
        consoleApi.AddCommand("instant_cooldown", ConsoleCommand.Create(ToggleSkillsCooldown, false));
        consoleApi.AddCommand("infinite_mana", ConsoleCommand.Create(cheatsApi.ToggleInfiniteMana, false));
        consoleApi.AddCommand("spirit_cooldown", ConsoleCommand.Create(SetSpiritCooldown, false));
        consoleApi.AddCommand("infinite_vessel", ConsoleCommand.Create(ToggleInfiniteVessel, false));
        consoleApi.AddCommand("infinite_transform", ConsoleCommand.Create(ToggleInfiniteTransform, false));
        consoleApi.AddCommand("arena", ConsoleCommand.Create(TeleportToArena, false));
        consoleApi.AddCommand("shrine", ConsoleCommand.Create(TeleportToShrine, false));
        consoleApi.AddCommand("pvp_level", ConsoleCommand.Create(TeleportToPvpLevel, true));
    }

    public void Dispose() { }

    private void RequestSpawn(string unitName, int count = 1)
    {
        if (syncApi.LocalMainCharacter is not { } player)
            return;

        var myTeam = player.TeamId;
        var teamId = PvpUtils.GetOppositeTeam(myTeam);
        var playerPawn = player.Pawn;
        if (playerPawn == null)
            return;

        var location = CalculateSpawnLocation(playerPawn.GetActorLocation(), playerPawn.GetActorForwardVector());

        syncApi.SpawnEnemy(new TamerKind(unitName), location.ToVector3(), count, teamId);

        var message = string.Format(PvpTexts.PlayerSpawned, player.Nickname, count, unitName);
        chatApi.SendServerMessage(message);
    }

    private static FVector CalculateSpawnLocation(FVector playerLocation, FVector playerForwardVector)
    {
        var spawnLoc = playerLocation + playerForwardVector * PvpConstants.MonsterSpawnDistance;

        var startLoc = spawnLoc + FVector.UpVector * PvpConstants.MonsterSpawnTraceHeight / 2;
        var endLoc = spawnLoc - FVector.UpVector * PvpConstants.MonsterSpawnTraceHeight / 2;

        // Trace vertically for spawn height.
        var hitResultSimple = new FHitResultSimple();
        var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), startLoc, endLoc, ref hitResultSimple);
        if (hit)
        {
            spawnLoc = hitResultSimple.HitLocation + FVector.UpVector * PvpConstants.MonsterHalfHeight;
        }

        return spawnLoc;
    }

    private void SetSpectatorStatus()
    {
        if (syncApi.LocalMainCharacter is not { } player)
            return;

        if (!pvpApi.InPvpTournament)
        {
            if (!player.IsSpectator)
            {
                syncApi.EnableSpectatorMode(player, SpectatorReason.Observer);
            }
            else
            {
                syncApi.DisableSpectatorMode(player);
            }
        }
    }

    private void SetSpiritCooldown(float spiritCooldownTime)
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea.HasValue && !areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            consoleApi.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        if (spiritCooldownTime < 0)
        {
            consoleApi.AddLocalizedMessage("InvalidCooldown");
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
        chatter.SendServerMessage("CustomSpiritCooldown", playerState.Nickname, spiritCooldownTime.ToString(CultureInfo.InvariantCulture));
    }

    private void ToggleInfiniteVessel()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea is { Room.CheatsAllowed: false })
        {
            consoleApi.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.FabaoEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.FabaoEnergyMax));
        }

        mainEntity.GetLocalState().HasInfiniteVessel = !mainEntity.GetLocalState().HasInfiniteVessel;
        chatter.SendServerMessage(mainEntity.GetLocalState().HasInfiniteVessel ? "InfVesselEnabled" : "InfVesselDisabled", playerState.Nickname);
    }

    private void ToggleInfiniteTransform()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea is { Room.CheatsAllowed: false })
        {
            consoleApi.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        if (mainEntity.HasPawn)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.CurEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.TransEnergyMax));
        }

        mainEntity.GetLocalState().HasInfiniteTransform = !mainEntity.GetLocalState().HasInfiniteTransform;
        var playerComp = mainEntity.GetState();
        chatter.SendServerMessage(mainEntity.GetLocalState().HasInfiniteTransform ? "InfTransformEnabled" : "InfTransformDisabled", playerComp.CharacterNickname);
    }

    private void ToggleSkillsCooldown()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.CurrentArea.HasValue && !areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            consoleApi.AddLocalizedMessage("CheatsAreDisabled");
            return;
        }

        ref var localStateComp = ref mainEntity.GetLocalState();
        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_ResetSkillCD.Invoke();
        localStateComp.InstantSkillCooldown = !localStateComp.InstantSkillCooldown;
        chatter.SendServerMessage(mainEntity.GetLocalState().InstantSkillCooldown ? "InstantCooldownEnabled" : "InstantCooldownDisabled", playerState.Nickname);
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
            consoleApi.AddMessage(BuiltinTexts.InvalidCommand);
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