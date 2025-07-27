using System;
using System.Collections.Generic;
using System.Linq;
using ReadyM.Api.ECS.Idents;
using ReadyM.Relay.Client.State;
using UnrealEngine.Engine;
using WukongMp.Api.Old.State;

namespace WukongMp.Api.Old;

public class WukongPlayerRegistry(ClientState state)
{
    private PlayerState? _localPlayerState;

    public PlayerState LocalPlayerState
    {
        get
        {
            if (_localPlayerState == null)
            {
                throw new InvalidOperationException("Local player state is null");
            }

            return _localPlayerState;
        }
        set => _localPlayerState = value;
    }

    public readonly Dictionary<PlayerId, PlayerState> ConnectedPlayers = new();

    public IEnumerable<PlayerState> AllConnectedPlayers
        => ConnectedPlayers.Values.Append(LocalPlayerState);

    public IEnumerable<PlayerState> SpectatingPlayers
        => ConnectedPlayers.Values.Where(p => p.IsSpectator).Concat(LocalPlayerState.IsSpectator ? [LocalPlayerState] : []);

    public IEnumerable<PlayerState> AllPvPPlayers
        => ConnectedPlayers.Values.Where(p => !p.IsSpectator).Concat(LocalPlayerState.IsSpectator ? [] : [LocalPlayerState]);

    public bool HasLocalPlayerState
        => _localPlayerState != null;

    public void ResetLocalPlayer()
    {
        _localPlayerState = null;
    }
    
    public void RegisterPlayer(PlayerState state)
    {
        Logging.LogDebug("Registering player {PlayerId}", state.PlayerId);
        ConnectedPlayers.Add(state.PlayerId, state);
    }

    public PlayerState? GetPlayerByActor(AActor? actor)
    {
        if (actor == null)
            return null;

        return actor == LocalPlayerState.Pawn
            ? LocalPlayerState
            : ConnectedPlayers.FirstOrDefault(x => x.Value!.Pawn == actor).Value;
    }

    [Obsolete]
    public PlayerState? GetPlayerById(PlayerId id)
    {
        return id == LocalPlayerState.PlayerId
            ? LocalPlayerState
            : ConnectedPlayers.GetValueOrDefault(id);
    }
}