using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using System;
using WukongMp.Api.State;

namespace WukongMp.Api;

public class GameStateSynchronizer : IDisposable
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
        var playerEntity = _playerState.LocalMainCharacter;
        if (playerEntity.HasValue)
        {
            BIC_MovieData? movieData = BGWGameInstanceCS.GetObject<BGW_GameDataMgr>(playerEntity.Value.GetLocalState().Pawn)?.GetGameInstanceWritableData<BIC_MovieData>();
            if (movieData != null)
            {
                Logging.LogDebug("Left area, cleaning up game state.");
                movieData.PlayMovieRequestQueue.Clear();
            }
        }
    }
}
