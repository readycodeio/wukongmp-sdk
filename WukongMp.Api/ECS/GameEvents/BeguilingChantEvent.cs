using System;
using ReadyM.Relay.Common.Mapping;
using ReadyM.Relay.Common.Wukong.ECS.Values;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): This is a server-side sent event
public readonly struct BeguilingChantEvent(BeguilingChantState state)
    : IEquatable<BeguilingChantEvent>, IAlwaysPropagatesToGameOnly
{
    public readonly BeguilingChantState State = state;

    public bool Equals(BeguilingChantEvent other)
        => State == other.State;

    public override bool Equals(object? obj)
        => obj is BeguilingChantEvent other && Equals(other);

    public override int GetHashCode()
        => State.GetHashCode();
}