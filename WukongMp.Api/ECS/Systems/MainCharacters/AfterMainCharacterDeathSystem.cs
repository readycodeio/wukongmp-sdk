using Friflo.Engine.ECS.Systems;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class AfterMainCharacterDeathSystem(WukongEventBus eventBus, WukongPlayerState playerState) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (playerState.LocalMainCharacter.HasValue)
        {
            var localMain = playerState.LocalMainCharacter.Value;
            if (localMain.GetState().IsDead && localMain.GetLocalState().IsDuringDeathAnim)
            {
                ref var localState = ref localMain.GetLocalState();
                localState.DeadAnimationTime -= Tick.deltaTime;
                if (localState.DeadAnimationTime <= 0f)
                {
                    localState.IsDuringDeathAnim = false;
                    PlayerUtils.EnableSpectator(localMain, SpectatorReason.Death);
                }
            }
        }
    }
}
