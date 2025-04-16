using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Client;
using Photon.Realtime;

namespace WukongApi.State
{
    public class RoomState(WukongClient client)
    {
        private Room Room => client.PhotonClient.CurrentRoom;

        public GameMode GameMode
        {
            get => (GameMode)GetProperty<int>(nameof(GameMode));
            set => SetProperty(nameof(GameMode), (int)value);
        }

        public int RoundsTotal
        {
            get => GetProperty<int>(nameof(RoundsTotal));
            set => SetProperty(nameof(RoundsTotal), value);
        }

        public bool InMatchmaking
        {
            get => GetProperty<bool>(nameof(InMatchmaking));
            set => SetProperty(nameof(InMatchmaking), value);
        }

        public long MatchmakingEndTime
        {
            get => GetProperty<long>(nameof(MatchmakingEndTime));
            set => SetProperty(nameof(MatchmakingEndTime), value);
        }

        public bool InPvP
        {
            get => GetProperty<bool>(nameof(InPvP));
            set => SetProperty(nameof(InPvP), value);
        }

        public bool InCombatRound
        {
            get => GetProperty<bool>(nameof(InCombatRound));
            set => SetProperty(nameof(InCombatRound), value);
        }

        public bool BotsEnabled
        {
            get => GetProperty<bool>(nameof(BotsEnabled));
            set => SetProperty(nameof(BotsEnabled), value);
        }

        public IEnumerable<int> RoundWinners
        {
            get
            {
                var str = GetProperty<string>(nameof(RoundWinners));
                return str == null ? [] : str.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
            }
            set => SetProperty(nameof(RoundWinners), string.Join(";", value));
        }

        public int CurrentRound => RoundWinners.Count() + 1;

        public void SetLastRoundWinnerTeam(int winner)
        {
            var winners = RoundWinners.ToList();
            winners.Add(winner);
            RoundWinners = winners;
        }

        private T? GetProperty<T>(string name)
        {
            if (Room.CustomProperties.TryGetValue(name, out var obj))
                return (T)obj;
            return default;
        }

        private void SetProperty(string name, object value)
        {
            var hash = new PhotonHashtable
            {
                [name] = value
            };
            Room.SetCustomProperties(hash);
        }
    }
}