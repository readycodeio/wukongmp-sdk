using Friflo.Engine.ECS.Systems;

namespace WukongMp.Api.ECS.Systems;

public class ReEnableCollidersSystem(ColliderDisableData colliderDisableData, WukongEventBus eventBus) : BaseSystem
{
    private const float TickIntervalSeconds = 1; // Check every second
    private float _elapsedTime;

    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        _elapsedTime += Tick.deltaTime;

        if (_elapsedTime < TickIntervalSeconds)
            return;

        colliderDisableData.TryReEnableColliders(_elapsedTime);
        _elapsedTime = 0f;
    }
}
