using System;
using System.Collections.Generic;
using System.Linq;

namespace WukongApi
{
    public class LobbyManager
    {
        public event Action<Action> PlayerTeleportRequested;
        public event Action OnRoundEnd;
        public bool CanPlayersJoin { get; private set; } = true;

        private readonly WukongClient _wukongClient;
        private readonly Dictionary<int, bool> _playersReady = new Dictionary<int, bool>();

        public LobbyManager(WukongClient wukongClient)
        {
            _wukongClient = wukongClient;
        }

        public void RegisterPlayerJoined(int playerId)
        {
            _playersReady.Add(playerId, false);
        }

        public void RegisterPlayerLeft(int playerId)
        {
            _playersReady.Remove(playerId);
        }

        public void SignalReadiness(int playerId, bool ready)
        {
            _playersReady[playerId] = ready;

            var playersReady = _playersReady.Count(pair => pair.Value);
            var allPlayers = _wukongClient.GetOtherPlayersInRoom().Count() + 1;

            GameUtils.ShowTip($"{playersReady}/{allPlayers} players are ready");
        }

        public void StartRound()
        {
            if (!_playersReady.GetValueOrDefault(_wukongClient.LocalPlayerState.PhotonId, false))
            {
                GameUtils.ShowTip("You must be ready to start the round");
                return;
            }

            foreach (var player in _wukongClient.GetOtherPlayersInRoom())
            {
                if (!_playersReady.GetValueOrDefault(player.ActorNumber, false))
                {
                    GameUtils.ShowTip($"Player '{player.NickName}' is not ready");
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