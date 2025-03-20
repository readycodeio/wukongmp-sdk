using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public class LobbyManager(WukongClient wukongClient)
    {
        public async Task StartRoundAsync()
        {
            PlacePlayers(Constants.PvpStartingLocation, Constants.PvpRadius);
            await Task.Delay(100);

            wukongClient.SendPvPEvent(PvPEvent.RoundStart);
        }

        private void PlacePlayers(FVector center, float radius)
        {
            var playerStates = wukongClient.AllPvPPlayers.ToList();

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
                wukongClient.BroadcastPlayerTransform(playerState.PhotonId, newPlayerLocation, UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0, 0, 500)));
            }
        }

        public async Task EndRoundAsync(int winner)
        {
            // disable pvp until next round
            wukongClient.SendPvPEvent(PvPEvent.RoundEnd, winner);

            // increment round number
            wukongClient.CurrentRoomState.SetLastRoundWinnerTeam(winner);

            // wait until all players death animations are finished
            await Task.Delay(5000);

            await ResetHpAndRespawnAllPlayers();

            // resolve tournament
            var winnersSoFar = wukongClient.CurrentRoomState.RoundWinners.ToList();
            var winnersByTeam = winnersSoFar.Where(w => w != Constants.DrawTeamId).GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

            // check if only one team is present
            if (wukongClient.AllPvPPlayers.Select(p => p.TeamId).Distinct().Count() == 1)
            {
                wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, wukongClient.LocalPlayerState.TeamId);
                return;
            }

            // check if any team won more than half of the rounds
            var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > wukongClient.CurrentRoomState.RoundsTotal / 2);
            if (winnerTeam.Key != 0)
            {
                wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, winnerTeam.Key);
                return;
            }

            // otherwise, check if we have a tie
            if (wukongClient.CurrentRoomState.CurrentRound > wukongClient.CurrentRoomState.RoundsTotal)
            {
                if (winnersByTeam.Count > 0)
                {
                    // if any team have won more than others
                    int maxWins = winnersByTeam.Values.Max();
                    var winningTeams = winnersByTeam.Where(t => t.Value == maxWins).Select(t => t.Key).ToList();
                    if (winningTeams.Count == 1)
                    {
                        wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, winningTeams[0]);
                    }
                    else
                    {
                        wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, Constants.DrawTeamId);
                    }
                }
                else
                {
                    // that was the final round
                    wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, Constants.DrawTeamId);
                }
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
            wukongClient.SendPvPEvent(PvPEvent.ResetStats);
            foreach (var player in wukongClient.AllConnectedPlayers)
            {
                if (player.IsDead)
                {
                    wukongClient.BroadcastPlayerRebirth(player.PhotonId);
                }
            }

            // wait for that to finish
            await Task.Delay(6500);
        }
    }
}