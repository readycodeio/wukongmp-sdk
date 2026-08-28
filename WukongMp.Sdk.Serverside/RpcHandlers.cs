using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.Rpc;

namespace WukongMp.Sdk.Serverside;

[ServerRpcFor(typeof(SdkRpcContracts))]
internal partial class RpcHandlers(EcsApi ecs, ILogger logger) : ServerRpcHandlersBase
{
    partial void OnPing(RpcContext context, long timestamp)
    {
        SendPing(context.Sender, timestamp);
    }

    private readonly Dictionary<int, HashSet<PlayerId>> _skipMovieRequests = new();

    partial void OnSkipMovie(RpcContext context, int sequenceId)
    {
        logger.LogDebug("Received skip movie request from player {PlayerId}, movie id {Id}", context.Sender, sequenceId);
        if (!_skipMovieRequests.TryGetValue(sequenceId, out var playerSet))
        {
            playerSet = [context.Sender];
            _skipMovieRequests[sequenceId] = playerSet;
        }
        else
        {
            playerSet.Add(context.Sender);
        }

        var connectedPlayers = 0;
        ecs.Query<MainCharacterComponent>((ref _) => { connectedPlayers++; });

        var response = new SkipMovieData
        {
            SequenceId = sequenceId,
            WaitingPlayers = playerSet.Count,
            AllPlayers = connectedPlayers
        };

        if (response.WaitingPlayers == response.AllPlayers)
        {
            logger.LogInformation("Skipping movie {Id} as all players requested it", sequenceId);
            _skipMovieRequests.Remove(sequenceId);
        }

        foreach (var playerId in playerSet)
        {
            SendSkipMovie(playerId, response);
        }
    }

    partial void OnMovieStarted(RpcContext context, int sequenceId, AreaId areaId)
    {
        ecs.Query<MovieComponent, AreaScopeComponent>((ref movie, ref area) =>
        {
            if (areaId == area.AreaId)
            {
                movie.AddStartedSequences(sequenceId);
                logger.LogDebug("Marked movie {Id} as started in area {AreaId}", sequenceId, areaId);
            }
        });
    }
}