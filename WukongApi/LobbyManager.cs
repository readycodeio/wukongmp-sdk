using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public class LobbyManager
    {
        private readonly WukongClient _wukongClient;

        public LobbyManager(WukongClient wukongClient)
        {
            _wukongClient = wukongClient;
        }

        public async Task StartRoundAsync()
        {
            foreach (var player in _wukongClient.ConnectedPlayers.Values)
            {
                if (!player.IsReadyForPvP)
                {
                    GameUtils.ShowTip($"Player {player.NickName} is not ready"); // TODO: Nickname
                    return;
                }
            }

            PlacePlayers(Constants.PvpStartingLocation, Constants.PvpRadius);
            await Task.Delay(500);

            _wukongClient.SendPvPEvent(PvPEvent.RoundStart);
        }

        private void PlacePlayers(FVector center, float radius)
        {
            var playerStates = _wukongClient.AllConnectedPlayers.ToList();

            var teamsIds = playerStates.Select(playerState => playerState.TeamId).Distinct().ToList();
            var teamsCount = teamsIds.Count;
            var teamAngleStep = 2 * MathF.PI / teamsCount;

            var entityOffsetAngle = 0.15f;
            var teamMemberIndex = new Dictionary<int, int>();
            var teamIndex = new Dictionary<int, int>();
            for (var i = 0; i < teamsIds.Count; i++)
            {
                teamMemberIndex[teamsIds[i]] = 0;
                teamIndex[teamsIds[i]] = i;
            }

            foreach (var playerState in playerStates)
            {
                var teamBaseAngle = teamIndex[playerState.TeamId] * teamAngleStep;
                var memberIndex = teamMemberIndex[playerState.TeamId];

                var angle = teamBaseAngle + (memberIndex + 1) * entityOffsetAngle;
                var x = center.X + radius * MathF.Cos(angle);
                var y = center.Y + radius * MathF.Sin(angle);

                teamMemberIndex[playerState.TeamId]++;
                var newPlayerLocation = new FVector(x, y, center.Z);
                _wukongClient.BroadcastPlayerTransform(playerState.PhotonId, newPlayerLocation, UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0,0,500)));
            }
        }

        public async Task EndRoundAsync(int winner)
        {
            // disable pvp until next round
            _wukongClient.SendPvPEvent(PvPEvent.RoundEnd, winner);

            // increment round number
            _wukongClient.CurrentRoomState.SetLastRoundWinnerTeam(winner);

            // wait until all players death animations are finished
            await Task.Delay(5000);

            await ResetHpAndRespawnAllPlayers();

            // resolve tournament
            var winnersSoFar = _wukongClient.CurrentRoomState.RoundWinners.ToList();
            var winnersByTeam = winnersSoFar.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

            // check if any team won more than half of the rounds
            var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > _wukongClient.CurrentRoomState.RoundsTotal / 2);
            if (winnerTeam.Key != 0)
            {
                _wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, winnerTeam.Key);
                return;
            }

            // otherwise, check if we have a tie
            if (_wukongClient.CurrentRoomState.CurrentRound > _wukongClient.CurrentRoomState.RoundsTotal)
            {
                // that was the final round
                _wukongClient.SendPvPEvent(PvPEvent.TournamentEnd);
            }
            else
            {
                // start next round
                await StartRoundAsync();
            }
        }

        private async Task ResetHpAndRespawnAllPlayers()
        {
            // resurrect dead players and restore health to living ones
            _wukongClient.SendPvPEvent(PvPEvent.ResetStats);
            foreach (var player in _wukongClient.AllConnectedPlayers)
            {
                if (player.IsDead)
                {
                    _wukongClient.BroadcastPlayerRebirth(player.PhotonId);
                }
            }

            // wait for that to finish
            await Task.Delay(6500);
        }
    }
}