using Friflo.Engine.ECS.Systems;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class FreeCameraMovementSystem(WukongPlayerState playerState, WukongEventBus eventBus, WukongAreaState areaState, FreeCameraManager freeCameraManager, FreeCameraMover freeCameraMover) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (!freeCameraManager.IsInFreeCameraMode)
            return;

        freeCameraMover.Update(Tick.deltaTime);

        //CalculateMouseRotate();

        //ExecMove();

        //ExecRotate();
    }


}
