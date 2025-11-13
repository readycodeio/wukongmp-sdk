using Friflo.Engine.ECS;
using ReadyM.Relay.Client.State;
using System;
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
#if DEBUG
        _wukongChatter.AddCommand("/spectator", new WukongChatterCommand(SetSpectatorStatus));
#endif
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
        if (args.Length == 1)
        {
            var isSpectator = args.Span[0].Equals("true", StringComparison.OrdinalIgnoreCase);

            var playerEntity = _playerState.LocalMainCharacter;
            if (playerEntity == null)
                return;
            playerEntity.Value.GetPvP().IsSpectator = isSpectator;
        }
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
