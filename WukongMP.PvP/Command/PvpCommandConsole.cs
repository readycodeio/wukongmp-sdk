using System;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.Configuration;
using WukongMp.Api.Command;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP.Command;

internal class PvpCommandConsole : IDisposable
{
    private readonly WukongCommandConsole _wukongCommandConsole;
    private readonly WukongChatter _wukongChatter;
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongAreaState _areaState;

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
        _wukongCommandConsole.AddCommand("/spawn", new ConsoleCommand(RequestSpawn));
        _wukongCommandConsole.AddCommand("/spectator", new ConsoleCommand(SetSpectatorStatus));
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
}
