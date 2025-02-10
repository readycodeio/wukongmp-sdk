using System;
using System.Linq;
using System.Threading.Tasks;

namespace WukongApi
{
    public class LobbyManager
    {
        public event Action PlayerTeleportRequested;
        public event Action OnRoundEnd;

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
            else if (playersReady == 1)
            {
                GameUtils.ShowTip("You are ready");
            }
            else if (playersReady == 2)
            {
                GameUtils.ShowTip("Both players are ready");
            }
            else
            {
                GameUtils.ShowTip($"All {playersReady} players are ready");
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

            PlayerTeleportRequested?.Invoke();

            Task.Run(async () =>
            {
                await Task.Delay(2000);
                _wukongClient.SendStartCountdown();
            });
        }

        public void EndRound(int winner)
        {
            _wukongClient.CurrentRoomState.SetLastRoundWinner(winner);
            OnRoundEnd?.Invoke();
        }

        public void DisplayRoundStartMessage()
        {
            var current = _wukongClient.CurrentRoomState.CurrentRound;
            var total = _wukongClient.CurrentRoomState.RoundsTotal;
            GameUtils.ShowTip($"Round {current} of {total}");
        }
    }
}