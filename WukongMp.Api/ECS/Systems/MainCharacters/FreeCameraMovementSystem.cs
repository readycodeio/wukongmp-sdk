using Friflo.Engine.ECS.Systems;
using WukongMp.Api.FreeCamera;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

internal class FreeCameraMovementSystem(WukongEventBus eventBus, FreeCameraManager freeCameraManager, FreeCameraController freeCameraController) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (!freeCameraManager.IsInFreeCameraMode)
            return;

        freeCameraController.Update(Tick.deltaTime);
    }
}
