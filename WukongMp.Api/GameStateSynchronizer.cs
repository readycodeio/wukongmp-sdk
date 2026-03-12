using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

internal class GameStateSynchronizer : IDisposable
{
    private readonly ClientState _state;
    private readonly WukongPlayerState _playerState;

    public GameStateSynchronizer(ClientState state, WukongPlayerState playerState)
    {
        _state = state;
        _playerState = playerState;

        _state.OnLeftArea += OnLeftAreaHandler;
    }

    public void Dispose()
    {
        _state.OnLeftArea -= OnLeftAreaHandler;
    }

    private void OnLeftAreaHandler(AreaId areaId, Entity entity)
    {
        Logging.LogDebug("Left area, cleaning up game state.");
        var playerEntity = _playerState.LocalMainCharacter;
        if (playerEntity.HasValue)
        {
            CutsceneUtils.ClearLocalJoiningCutsceneStatus(playerEntity.Value);
        }
    }
}
