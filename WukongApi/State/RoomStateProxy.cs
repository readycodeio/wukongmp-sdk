using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReadyM.Relay.Client;

namespace WukongApi.State
{
    public sealed class RoomStateProxy(RelayClient client) : RoomStateProxyBase(client)
    {
        public GameMode GameMode
        {
            get => (GameMode)GetProperty<int>(nameof(GameMode));
            set => SetProperty(nameof(GameMode), (int)value);
        }

        public int TournamentRounds
        {
            get => GetProperty<int>(nameof(TournamentRounds));
            set => SetProperty(nameof(TournamentRounds), value);
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

        public bool GourdAllowed
        {
            get => GetProperty<bool>(nameof(GourdAllowed));
            set => SetProperty(nameof(GourdAllowed), value);
        }

        public bool ConsumablesAllowed
        {
            get => GetProperty<bool>(nameof(ConsumablesAllowed));
            set => SetProperty(nameof(ConsumablesAllowed), value);
        }

        public bool ImmobilizeAllowed
        {
            get => GetProperty<bool>(nameof(ImmobilizeAllowed));
            set => SetProperty(nameof(ImmobilizeAllowed), value);
        }

        public bool PhantomRushAllowed
        {
            get => GetProperty<bool>(nameof(PhantomRushAllowed));
            set => SetProperty(nameof(PhantomRushAllowed), value);
        }

        public int EnemiesNgPlusLevel
        {
            get => GetProperty<int>(nameof(EnemiesNgPlusLevel));
            set => SetProperty(nameof(EnemiesNgPlusLevel), value);
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

        public int NextMonsterId
        {
            get => GetProperty<int>(nameof(NextMonsterId));
            set => SetProperty(nameof(NextMonsterId), value);
        }

        public void SetLastRoundWinnerTeam(int winner)
        {
            var winners = RoundWinners.ToList();
            winners.Add(winner);
            RoundWinners = winners;
        }

        public override string ToString()
        {
            // print every single property
            var properties = GetType().GetProperties();
            var lines = new List<string>();
            foreach (var property in properties)
            {
                var value = property.GetValue(this);
                if (value is IEnumerable<int> enumerable)
                {
                    lines.Add($"{property.Name}: {string.Join(", ", enumerable)}");
                }
                else
                {
                    lines.Add($"{property.Name}: {value}");
                }
            }

            lines.Sort();

            var sb = new StringBuilder();
            sb.AppendLine("-------------------------");
            sb.AppendLine("ROOM STATE:");

            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}