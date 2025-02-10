using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnrealEngine.Runtime;
using System.Linq;
using UnrealEngine.Engine;

namespace WukongApi
{
    public class LobbyManager
    {
        private readonly WukongClient _wukongClient;

        public LobbyManager(WukongClient wukongClient)
        {
            _wukongClient = wukongClient;
        }

        public void StartRound()
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

            Task.Run(async () =>
            {
                await Task.Delay(2000);
                _wukongClient.SendStartCountdown();
            });
        }

        private void PlacePlayers(FVector center, float radius)
        {
            var playerStates = _wukongClient.ConnectedPlayers.Values.ToList();
            playerStates.Add(_wukongClient.LocalPlayerState);

            var teamsIds = playerStates.Select(playerState => playerState.TeamId).Distinct();
            var teamsCount = teamsIds.Count();
            float teamAngleStep = 2 * MathF.PI / teamsCount;

            float entityOffsetAngle = 0.1f;
            Dictionary<int, int> teamMemberIndex = new Dictionary<int, int>();
            foreach (var teamId in teamsIds)
            {
                teamMemberIndex[teamId] = 0;
            }

            foreach (var playerState in playerStates)
            {
                float teamBaseAngle = playerState.TeamId * teamAngleStep;
                int memberIndex = teamMemberIndex[playerState.TeamId];

                float angle = teamBaseAngle + (memberIndex + 1) * entityOffsetAngle;
                float x = center.X + radius * MathF.Cos(angle);
                float y = center.Y + radius * MathF.Sin(angle);

                teamMemberIndex[playerState.TeamId]++;
                playerState.Location = new FVector(x, y, center.Z);
                playerState.Rotation = UMathLibrary.FindLookAtRotation(playerState.Location, center);
                playerState.Pawn.SetActorTransform(new FTransform(playerState.Rotation, playerState.Location), false, out _, true);
            }
        }

        public void EndRound(int winner)
        {
            // increment round number
            _wukongClient.CurrentRoomState.SetLastRoundWinnerTeam(winner);

            // resurrect dead players
            foreach (var (id, player) in _wukongClient.ConnectedPlayers)
            {
                if (player.IsDead)
                {
                    _wukongClient.BroadcastPlayerRebirth(id);
                }
            }

            if (_wukongClient.CurrentRoomState.CurrentRound < _wukongClient.CurrentRoomState.RoundsTotal)
            {
                StartRound();
            }
        }
    }
}