using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongMp.Api
{
    public class LobbyManager(WukongClient wukongClient)
    {
        private bool _isRoundEnding;

        public async Task StartRoundAsync()
        {
            if (!wukongClient.IsMasterClient)
            {
                Logging.LogError("Only master client can use the lobby manager");
                return;
            }

            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            PlacePlayers(levelData.PvpStartingLocation, levelData.PvpRadius);
            await Task.Delay(100);

            wukongClient.SendPvPEvent(PvPEvent.RoundStart);
        }

        private void PlacePlayers(FVector center, float radius)
        {
            if (!wukongClient.IsMasterClient)
            {
                Logging.LogError("Only master client can use the lobby manager");
                return;
            }

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
                var newPlayerLocation = GameUtils.GetFinalLocation(playerState.Pawn, new FVector(x, y, center.Z));
                wukongClient.BroadcastPlayerTransform(playerState.PeerId, newPlayerLocation, UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0, 0, 500)));
            }
        }

        public async Task EndRoundAsync(int winner)
        {
            if (!wukongClient.IsMasterClient)
            {
                Logging.LogError("Only master client can use the lobby manager");
                return;
            }

            if (_isRoundEnding)
            {
                return;
            }

            _isRoundEnding = true;

            // disable pvp until next round
            wukongClient.SendPvPEvent(PvPEvent.RoundEnd, winner);

            // increment round number
            wukongClient.RoomState.SetLastRoundWinnerTeam(winner);

            // wait until all players death animations are finished
            await Task.Delay(5000);

            if (!wukongClient.IsMasterClient)
            {
                Logging.LogDebug("Master client disconnected before finishing EndRoundAsync");
                return;
            }

            await ResetHpAndRespawnAllPlayers();

            // resolve tournament
            var winnersSoFar = wukongClient.RoomState.RoundWinners.ToList();
            var winnersByTeam = winnersSoFar.Where(w => w != Constants.DrawTeamId).GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

            // check if only one team is present
            if (wukongClient.AllPvPPlayers.Select(p => p.TeamId).Distinct().Count() == 1)
            {
                wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, winner);
                _isRoundEnding = false;
                return;
            }

            // check if any team won more than half of the rounds
            var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > wukongClient.RoomState.TournamentRounds / 2);
            if (winnerTeam.Key != 0)
            {
                wukongClient.SendPvPEvent(PvPEvent.TournamentEnd, winnerTeam.Key);
                _isRoundEnding = false;
                return;
            }

            // otherwise, check if we have a tie
            if (wukongClient.RoomState.CurrentRound > wukongClient.RoomState.TournamentRounds)
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

            _isRoundEnding = false;
        }

        private async Task ResetHpAndRespawnAllPlayers()
        {
            if (!wukongClient.IsMasterClient)
            {
                Logging.LogError("Only master client can use the lobby manager");
                return;
            }

            // resurrect dead players and restore health to living ones
            wukongClient.SendPvPEvent(PvPEvent.ResetStats);
            foreach (var player in wukongClient.AllConnectedPlayers)
            {
                if (player.IsDead)
                {
                    wukongClient.BroadcastPlayerRebirth(player.PeerId);
                }
            }

            // wait for that to finish
            await Task.Delay(6500);
        }
    }
}