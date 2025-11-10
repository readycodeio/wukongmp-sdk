using System;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.PvP.Chat
{
    internal class PvpChatter
    {
        private readonly WukongChatter _wukongChatter;
        private readonly WukongPlayerState _playerState;
        private readonly WukongRpcCallbacks _rpc;

        public PvpChatter(
        WukongChatter wukongChatter,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc
    )
        {
            Logging.LogDebug("Initializing WukongChatter");

            _wukongChatter = wukongChatter;
            _playerState = playerState;
            _rpc = rpc;

            SetupCommands();
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
                ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidUnitName}: \"{args.Span[0]}\"");
                return;
            }

            var playerEntity = _playerState.LocalPlayerEntity;
            if (playerEntity == null)
                return;

            var characterEntity = _playerState.LocalMainCharacter;
            if (characterEntity == null)
                return;

            var teamId = PvPUtils.GetOppositeTeam(playerEntity.Value.GetState().TeamId);
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
                            ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidUnitName}: \"{args.Span[1]}\"");
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
    }
}
