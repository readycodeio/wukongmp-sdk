using Friflo.Engine.ECS.Systems;
using ReadyM.Wukong.Common.ECS.Values;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

internal class AfterMainCharacterDeathSystem(WukongEventBus eventBus, WukongPlayerState playerState) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (playerState.LocalMainCharacter.HasValue)
        {
            var localMain = playerState.LocalMainCharacter.Value;
            if (localMain.GetHp().IsDead && localMain.GetLocalState().IsDuringDeathAnim)
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
