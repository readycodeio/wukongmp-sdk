using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Server.Entity;
using ReadyM.Wukong.Common.Rpc;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Serverside;

[ServerRpcFor(typeof(SdkRpcContracts))]
internal partial class RpcHandlers(IEntities ecs, ILogger logger) : ServerRpcHandlersBase
{
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
        
        foreach (var _ in ecs.Query<MainCharacter>())
        {
            connectedPlayers++;
        }
        
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
        foreach (var area in ecs.Query<Area>())
        {
            if (areaId == area.AreaId)
            {
                // TODO: Support for native collection accessors
                // movie.AddStartedSequences(sequenceId);
                logger.LogDebug("Marked movie {Id} as started in area {AreaId}", sequenceId, areaId);
            }
        }
    }
}