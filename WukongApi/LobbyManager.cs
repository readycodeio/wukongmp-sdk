using System;
using System.Linq;
using System.Threading.Tasks;

namespace WukongApi
{
    public class LobbyManager
    {
        private readonly WukongClient _wukongClient;

        public LobbyManager(WukongClient wukongClient)
        {
            _wukongClient = wukongClient;
        }

        public void DisplayReadinessChangeTips()
        {
            var players = _wukongClient.ConnectedPlayers.Values;
            var playersReady = players.Count(x => x.IsReadyForPvP) + (_wukongClient.LocalPlayerState.IsReadyForPvP ? 1 : 0);
            var allPlayers = players.Count + 1;

            if (playersReady != allPlayers)
            {
                GameUtils.ShowTip($"{playersReady}/{allPlayers} players are ready");
            }
            else
            {
                switch (playersReady)
                {
                    case 1:
                        GameUtils.ShowTip("You are ready");
                        break;
                    case 2:
                        GameUtils.ShowTip("Both players are ready");
                        break;
                    default:
                        GameUtils.ShowTip($"All {playersReady} players are ready");
                        break;
                }
            }
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

            // TODO: Teleport players

            Task.Run(async () =>
            {
                await Task.Delay(2000);
                _wukongClient.SendStartCountdown();
            });
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

        public void DisplayRoundStartMessage()
        {
            var current = _wukongClient.CurrentRoomState.CurrentRound;
            var total = _wukongClient.CurrentRoomState.RoundsTotal;
            GameUtils.ShowTip($"Round {current} of {total}");
        }
    }
}