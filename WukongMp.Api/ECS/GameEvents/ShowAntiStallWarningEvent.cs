using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct ShowAntiStallWarningEvent(int warningTime) : IAlwaysPropagatesToEcsOnly
{
    public int WarningTime { get; } = warningTime;
}