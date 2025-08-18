using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

/// <summary>
/// Deletes the MainCharacterEntity corresponding to the locally controlled pawn when the pawn gets destroyed.
/// </summary>
/// <param name="playerState"></param>
public class DeleteLocalMainCharacterEntitySystem(WukongPlayerState playerState, ILogger logger) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        var pawn = GameUtils.GetControlledPawn();
        var mainEntity = playerState.LocalMainCharacter;

        if (pawn.IsNullOrDestroyed() && mainEntity != null)
        {
            logger.LogDebug("DELETING LOCAL MAIN CHARACTER ENTITY");
            // NOTE: Controlled pawn doesn't exist (perhaps unloading scene) so we need to bring ECS up to date
            playerState.DeleteLocalMainCharacter();
        }
    }
}
