using Friflo.Engine.ECS;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

internal class CutsceneStatusSynchronizer(ClientState state, WukongPlayerState playerState) : IHostedService
{
    public void OnScopeStart()
    {
        state.OnLeftArea += OnLeftAreaHandler;
    }

    public void Dispose()
    {
        state.OnLeftArea -= OnLeftAreaHandler;
    }

    private void OnLeftAreaHandler(AreaId areaId, Entity entity)
    {
        Logging.LogDebug("Left area, cleaning up game state.");
        var playerEntity = playerState.LocalMainCharacter;
        if (playerEntity.HasValue)
        {
            CutsceneUtils.ClearLocalJoiningCutsceneStatus(playerEntity.Value);
        }
    }
}