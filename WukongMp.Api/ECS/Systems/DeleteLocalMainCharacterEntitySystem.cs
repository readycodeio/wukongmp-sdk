using Friflo.Engine.ECS.Systems;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public class DeleteLocalMainCharacterEntitySystem(WukongPlayerState playerState) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        var pawn = GameUtils.GetControlledPawn();
        var mainEntity = playerState.LocalMainCharacter;

        if (pawn.IsNullOrDestroyed() && mainEntity != null)
        {
            // NOTE: Controlled pawn doesn't exist (perhaps unloading scene) so we need to bring ECS up to date
            playerState.DeleteLocalMainCharacter();
        }
    }
}
