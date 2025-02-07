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

        public RoomState(WukongClient client)
        {
            _client = client;
        }

        private Room Room => _client.PhotonClient.CurrentRoom;

        private void SetProperty(string name, object value)
        {
            var hash = new PhotonHashtable
            {
                [name] = value
            };
            Room.SetCustomProperties(hash);
        }

        private T GetProperty<T>(string name)
        {
            if (Room.CustomProperties.TryGetValue(name, out var obj))
                return (T)obj;
            return default;
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
        }

        public void SetLastRoundWinner(int winner)
        {
            var winners = RoundWinners.ToList();
            winners.Add(winner);
            SetProperty(nameof(RoundWinners), string.Join(";", winners));
        }

        public int CurrentRound => RoundWinners.Count() + 1;
    }
}