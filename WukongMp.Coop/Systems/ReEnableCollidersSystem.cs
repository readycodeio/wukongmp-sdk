using Friflo.Engine.ECS;
using WukongMp.Sdk;

namespace WukongMp.Coop.Systems;

public sealed class ReEnableCollidersSystem(ColliderDisableData colliderDisableData) : ModSystemBase
{
    private const float TickIntervalSeconds = 1; // Check every second
    private float _elapsedTime;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!LocalApi.IsGameplayLevel)
            return;

        _elapsedTime += tick.deltaTime;

        if (_elapsedTime < TickIntervalSeconds)
            return;

        colliderDisableData.TryReEnableColliders(_elapsedTime);
        _elapsedTime = 0f;
    }
}