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
            foreach (var playerState in _wukongClient.ConnectedPlayers.Values)
            {
                playerState.Pawn.SetActorLocation(Constants.StartingLocation, false, out _, true);
            }

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