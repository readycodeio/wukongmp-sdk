using System;

namespace WukongMp.Api.GameApi
{
    public readonly struct CharacterId(int index) : IEquatable<CharacterId>
    {
        private readonly int _index = index + 1;

        public int Index
            => _index - 1;

        public bool Equals(CharacterId other)
            => _index == other._index;

        public override bool Equals(object? obj)
            => obj is CharacterId other && Equals(other);

        public override int GetHashCode()
            => _index;

        public static bool operator ==(CharacterId x, CharacterId y)
            => x._index == y._index;

        public static bool operator !=(CharacterId x, CharacterId y)
            => x._index != y._index;

        public override string ToString()
            => _index == default
                ? "CharacterId.Null"
                : $"CharacterId({Index})";
    }
}