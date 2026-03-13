using Friflo.Engine.ECS;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public sealed class ReEnableCollidersSystem : ModSystemBase
{
    private const float TickIntervalSeconds = 1; // Check every second
    private float _elapsedTime;
    private readonly ColliderDisableData _colliderDisableData;

    public ReEnableCollidersSystem()
    {
        _colliderDisableData = new ColliderDisableData(Logger);
    }

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Local.IsGameplayLevel)
            return;

        _elapsedTime += tick.deltaTime;

        if (_elapsedTime < TickIntervalSeconds)
            return;

        _colliderDisableData.TryReEnableColliders(_elapsedTime);
        _elapsedTime = 0f;
    }
}