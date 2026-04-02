using System;

namespace WukongMp.Api.Configuration;

public readonly struct TamerKind : IEquatable<TamerKind>
{
    public readonly string? Name;

    internal TamerKind(string? name)
    {
        Name = name;
    }

    public bool Equals(TamerKind other)
        => Name == other.Name;

    public override bool Equals(object? obj)
        => obj is TamerKind other && Equals(other);

    public override int GetHashCode()
        => Name?.GetHashCode() ?? 0;

    public override string ToString()
        => $"TamerKind[{Name}]";

    public static bool operator ==(TamerKind left, TamerKind right)
        => left.Equals(right);
    
    public static bool operator !=(TamerKind left, TamerKind right)
        => !left.Equals(right);
}