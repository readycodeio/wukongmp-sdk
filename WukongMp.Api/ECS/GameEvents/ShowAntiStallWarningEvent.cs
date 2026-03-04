using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public struct ShowAntiStallWarningEvent(int warningTime) : IAlwaysPropagatesToEcsOnly
{
    public int WarningTime { get; } = warningTime;
}