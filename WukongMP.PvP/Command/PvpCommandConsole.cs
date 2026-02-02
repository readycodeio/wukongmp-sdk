using b1;
using BtlShare;
using System;
using System.Globalization;
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

internal class PvpCommandConsole : IDisposable
{
    private readonly WukongCommandConsole _wukongCommandConsole;
    private readonly WukongChatter _wukongChatter;
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongAreaState _areaState;
    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";

    public PvpCommandConsole(
        WukongCommandConsole wukongCommandConsole,
        WukongChatter wukongChatter,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc,
        WukongAreaState areaState
    )
    {
        Logging.LogDebug("Initializing PvpCommandConsole");

        _wukongCommandConsole = wukongCommandConsole;
        _wukongChatter = wukongChatter;
        _playerState = playerState;
        _rpc = rpc;
        _areaState = areaState;

        SetupCommands();
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing PvpCommandConsole");
    }

    private void SetupCommands()
    {
        _wukongCommandConsole.AddCommand("/spawn", new ConsoleCommand(RequestSpawn), UnitPathsConfig.GetAllValidUnitNames());
        _wukongCommandConsole.AddCommand("/spectator", new ConsoleCommand(SetSpectatorStatus));
        _wukongCommandConsole.AddCommand("/instant_cooldown", new ConsoleCommand(ToggleSkillsCooldown));
        _wukongCommandConsole.AddCommand("/infinite_mana", new ConsoleCommand(ToggleInfiniteMana));
        _wukongCommandConsole.AddCommand("/spirit_cooldown", new ConsoleCommand(SetSpiritCooldown));
        _wukongCommandConsole.AddCommand("/infinite_vessel", new ConsoleCommand(ToggleInfiniteVessel));
        _wukongCommandConsole.AddCommand("/arena", new ConsoleCommand(TeleportToArena));
        _wukongCommandConsole.AddCommand("/shrine", new ConsoleCommand(TeleportToShrine));
#if DEBUG
        _wukongCommandConsole.AddCommand("/pvp_level", new ConsoleCommand(TeleportToPvpLevel));
#endif
    }

    private void RequestSpawn(ReadOnlyMemory<string> args)
    {
        var unitName = args.Span[0];
        if (!UnitPathsConfig.IsValidUnitName(unitName))
        {
            _wukongCommandConsole.AddMessageToConsole(string.Format(Texts.InvalidUnitName, args.Span[0]));
            return;
        }

        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;

        var characterEntity = _playerState.LocalMainCharacter;
        if (characterEntity == null)
            return;

        var teamId = PvpUtils.GetOppositeTeam(playerEntity.Value.GetState().TeamId);
        var playerPawn = characterEntity.Value.GetLocalState().Pawn;
        if (playerPawn == null)
            return;

        var location = SpawningUtils.CalculateSpawnLocation(playerPawn.GetActorLocation(), playerPawn.GetActorForwardVector());
        var count = 0;
        var shouldSpawn = false;

        switch (args.Length)
        {
            case 1:
                count = 1;
                shouldSpawn = true;
                break;
            case 2:
            {
                if (int.TryParse(args.Span[1], out count))
                {
                    shouldSpawn = true;
                }
                else
                {
                    _wukongCommandConsole.AddMessageToConsole(string.Format(Texts.InvalidUnitsCount, args.Span[1]));
                }

                break;
            }
        }

        if (shouldSpawn)
        {
            _rpc.SendRequestSpawnUnits(new UnitSpawnRequestData(unitName, count, teamId, location));
            _wukongChatter.SendServerMessage("PlayerSpawned", characterEntity.Value.GetState().CharacterNickName, count.ToString(), args.Span[0]);
        }
    }

    private void SetSpectatorStatus(ReadOnlyMemory<string> args)
    {
        if (args.Length == 0)
        {
            var playerEntity = _playerState.LocalMainCharacter;
            if (playerEntity == null)
                return;

            if (!_areaState.PvpState!.Value.InTournament)
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
    }

    private void ToggleInfiniteMana(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.CurrentArea.HasValue && !_areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            _wukongCommandConsole.AddLocalizedMessageToConsole("CheatsAreDisabled");
            return;
        }

        ref var localState = ref mainEntity.GetLocalState();
        if (localState.Pawn != null)
        {
            PlayerUtils.ResetMana(localState.Pawn);
        }

        localState.HasInfiniteMana = !localState.HasInfiniteMana;
        _wukongChatter.SendServerMessage(mainEntity.GetLocalState().HasInfiniteMana ? "InfManaEnabled" : "InfManaDisabled", NickName);
    }

    private void SetSpiritCooldown(ReadOnlyMemory<string> args)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.CurrentArea.HasValue && !_areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            _wukongCommandConsole.AddLocalizedMessageToConsole("CheatsAreDisabled");
            return;
        }

        if (args.Length < 1)
        {
            _wukongCommandConsole.AddLocalizedMessageToConsole("InvalidCooldown");
            return;
        }

        bool success = float.TryParse(args.Span[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float spiritCooldownTime);
        if (!success || spiritCooldownTime < 0)
        {
            _wukongCommandConsole.AddLocalizedMessageToConsole("InvalidCooldown");
            return;
        }

        ref var localState = ref mainEntity.GetLocalState();
        if (localState.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(localState.Pawn);
            mainEntity.GetLocalState().ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(localState.Pawn, EBGUAttrFloat.VigorEnergyMax));
            mainEntity.GetLocalState().ShouldSetSpiritCooldown = false;
        }

        mainEntity.GetLocalState().SpiritCooldownEnabled = true;
        mainEntity.GetLocalState().SpiritCooldownTime = spiritCooldownTime;
        _wukongChatter.SendServerMessage("CustomSpiritCooldown", NickName, spiritCooldownTime.ToString());
    }

    private void ToggleInfiniteVessel(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.CurrentArea.HasValue && !_areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            _wukongCommandConsole.AddLocalizedMessageToConsole("CheatsAreDisabled");
            return;
        }

        ref var localState = ref mainEntity.GetLocalState();
        if (localState.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(localState.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.FabaoEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(localState.Pawn, EBGUAttrFloat.FabaoEnergyMax));
        }

        mainEntity.GetLocalState().HasInfiniteVessel = !mainEntity.GetLocalState().HasInfiniteVessel;
        _wukongChatter.SendServerMessage(mainEntity.GetLocalState().HasInfiniteVessel ? "InfVesselEnabled" : "InfVesselDisabled", NickName);
    }

    private void ToggleSkillsCooldown(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.CurrentArea.HasValue && !_areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            _wukongCommandConsole.AddLocalizedMessageToConsole("CheatsAreDisabled");
            return;
        }

        ref var localState = ref mainEntity.GetLocalState();
        var events = BUS_EventCollectionCS.Get(localState.Pawn);
        events?.Evt_ResetSkillCD.Invoke();
        localState.InstantSkillCooldown = !localState.InstantSkillCooldown;
        _wukongChatter.SendServerMessage(mainEntity.GetLocalState().InstantSkillCooldown ? "InstantCooldownEnabled" : "InstantCooldownDisabled", NickName);
    }

    private void TeleportToArena(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.InRoom && !mainEntity.GetPvP().IsSpectator && _areaState.PvpState is { InTournament: false })
        {
            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            PlayerUtils.TeleportLocalPlayer(mainEntity, levelData.PvpStartingLocation, FRotator.ZeroRotator);
        }
    }

    private void TeleportToShrine(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.InRoom && !mainEntity.GetPvP().IsSpectator && _areaState.PvpState is { InTournament: false })
        {
            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            PlayerUtils.TeleportLocalPlayerToRebirthPoint(mainEntity, levelData.BirthPointID);
        }
    }

    private void TeleportToPvpLevel(ReadOnlyMemory<string> args)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity || !_areaState.InRoom || mainEntity.GetPvP().IsSpectator || _areaState.PvpState is { InTournament: true })
            return;

        if (args.Length < 1)
        {
            _wukongCommandConsole.AddMessageToConsole(Texts.InvalidCommand);
            return;
        }

        bool success = int.TryParse(args.Span[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pvpLevelId);
        if (!success || pvpLevelId < 0)
        {
            _wukongCommandConsole.AddMessageToConsole(Texts.InvalidCommand);
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
