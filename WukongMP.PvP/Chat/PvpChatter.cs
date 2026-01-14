using b1;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.State;
using System;
using System.Globalization;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP.Chat;

internal class PvpChatter : IDisposable
{
    private readonly WukongChatter _wukongChatter;
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly GameplayEventRouter _eventRouter;
    private readonly WukongAreaState _areaState;
    private readonly WukongPawnState _pawnState;
    private readonly ClientOwnershipManager _clientOwnership;
    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";

    public PvpChatter(
        WukongChatter wukongChatter,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc,
        GameplayEventRouter eventRouter,
        WukongAreaState areaState,
        WukongPawnState pawnState,
        ClientOwnershipManager clientOwnership
    )
    {
        Logging.LogDebug("Initializing PvpChatter");

        _wukongChatter = wukongChatter;
        _playerState = playerState;
        _rpc = rpc;
        _eventRouter = eventRouter;
        _areaState = areaState;
        _pawnState = pawnState;
        _clientOwnership = clientOwnership;

        _eventRouter.OnUnitDead += OnUnitDead;

        SetupCommands();
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing PvpChatter");

        _eventRouter.OnUnitDead -= OnUnitDead;
    }

    private void SetupCommands()
    {
        _wukongChatter.AddCommand("/spawn", new WukongChatterCommand(RequestSpawn));
        _wukongChatter.AddCommand("/spectator", new WukongChatterCommand(SetSpectatorStatus));
        _wukongChatter.AddCommand("/instant_cooldown", new WukongChatterCommand(ToggleSkillsCooldown));
        _wukongChatter.AddCommand("/infinite_mana", new WukongChatterCommand(ToggleInfiniteMana));
        _wukongChatter.AddCommand("/spirit_cooldown", new WukongChatterCommand(SetSpiritCooldown));
        _wukongChatter.AddCommand("/infinite_vessel", new WukongChatterCommand(ToggleInfiniteVessel));
    }

    private void RequestSpawn(ReadOnlyMemory<string> args)
    {
        var unitName = args.Span[0];
        if (!UnitPathsConfig.IsValidUnitName(unitName))
        {
            _wukongChatter.AddLocalCommandMessage($"${Texts.InvalidUnitName}: \"{args.Span[0]}\"");
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
                    _wukongChatter.AddLocalCommandMessage($"{Texts.InvalidUnitName}: \"{args.Span[1]}\"");
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
                pvp.IsSpectator = !pvp.IsSpectator;
            }
        }
    }

    private void ToggleInfiniteMana(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (_areaState.CurrentArea.HasValue && !_areaState.CurrentArea.Value.Room.CheatsAllowed)
        {
            _wukongChatter.AddLocalServerMessage("CheatsAreDisabled");
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
            _wukongChatter.AddLocalServerMessage("CheatsAreDisabled");
            return;
        }

        bool success = float.TryParse(args.Span[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float spiritCooldownTime);
        if (!success)
        {
            _wukongChatter.AddLocalServerMessage("InvalidCooldown");
            return;
        }

        ref var localState = ref mainEntity.GetLocalState();
        if (localState.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(localState.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(localState.Pawn, EBGUAttrFloat.VigorEnergyMax));
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
            _wukongChatter.AddLocalServerMessage("CheatsAreDisabled");
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
            _wukongChatter.AddLocalServerMessage("CheatsAreDisabled");
            return;
        }

        ref var localState = ref mainEntity.GetLocalState();
        var events = BUS_EventCollectionCS.Get(localState.Pawn);
        events?.Evt_ResetSkillCD.Invoke();
        localState.InstantSkillCooldown = !localState.InstantSkillCooldown;
        _wukongChatter.SendServerMessage(mainEntity.GetLocalState().InstantSkillCooldown ? "InstantCooldownEnabled" : "InstantCooldownDisabled", NickName);
    }

    private void OnUnitDead(Entity victim, Entity attacker)
    {
        if (_areaState is { PvpState.InPvP: true })
        {
            if (victim != attacker)
            {
                if (_pawnState.TryGetMainCharacterEntity(victim, out var victimMainEntity) &&
                    _pawnState.TryGetMainCharacterEntity(attacker, out var attackerMainEntity))
                {
                    if (!_clientOwnership.OwnsEntity(victimMainEntity.Value.Entity))
                        return;

                    ref var attackerMain = ref attackerMainEntity.Value.GetState();
                    ref var killedMain = ref victimMainEntity.Value.GetState();

                    _wukongChatter.SendServerMessage("PlayerKilledPlayer", attackerMain.CharacterNickName, killedMain.CharacterNickName);
                }
            }
        }
    }
}