using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public struct ShowAntiStallWarningEvent(int warningTime) : IAlwaysPropagatesToEcsOnly
{
    public int WarningTime { get; } = warningTime;
}