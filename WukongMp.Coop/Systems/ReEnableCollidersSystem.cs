using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public sealed class ReEnableCollidersSystem(ColliderDisableData data) : ModSystemBase
{
    private const float TickIntervalSeconds = 1; // Check every second
    private float _elapsedTime;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Local.IsGameplayLevel)
            return;

        _elapsedTime += tick.deltaTime;

        if (_elapsedTime < TickIntervalSeconds)
            return;

        data.TryReEnableColliders(_elapsedTime);
        _elapsedTime = 0f;
    }
}