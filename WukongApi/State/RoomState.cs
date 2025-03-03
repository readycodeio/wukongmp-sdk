using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Client;
using Photon.Realtime;

namespace WukongApi.State
{
    public class RoomState
    {
        private readonly WukongClient _client;

        private Room Room => _client.PhotonClient.CurrentRoom;

        public RoomState(WukongClient client)
        {
            _client = client;
        }

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

        public IEnumerable<int> RoundWinners
        {
            get
            {
                var str = GetProperty<string>(nameof(RoundWinners));
                return str == null ? Enumerable.Empty<int>() : str.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
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

        private T GetProperty<T>(string name)
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