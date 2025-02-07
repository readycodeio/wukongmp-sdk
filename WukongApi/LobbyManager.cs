using System;
using System.Collections.Generic;
using System.Linq;
using WukongApi.State;

namespace WukongApi
{
    public class LobbyManager
    {
        public event Action<Action> PlayerTeleportRequested;
        public event Action OnRoundEnd;
        public bool CanPlayersJoin { get; private set; } = true;

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

            CanPlayersJoin = false;
            PlayerTeleportRequested?.Invoke(PlayersTeleported);
        }

        private void PlayersTeleported()
        {
            _wukongClient.SendStartCountdown();
        }

        public void EndRound()
        {
            OnRoundEnd?.Invoke();
            CanPlayersJoin = true;
        }
    }
}