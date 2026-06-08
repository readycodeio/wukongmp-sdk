using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.Rpc;

namespace WukongMp.Sdk.Serverside;

public partial class RpcHandlers(EcsApi ecs, RpcApi rpc, ILogger logger) : ServerRpcHandlersBase(rpc)
{
    partial void OnPing(RpcContext context, long timestamp)
    {
        SendPing(context.Sender, timestamp);
    }
    
    private readonly Dictionary<int, HashSet<PlayerId>> _skipMovieRequests = new();

    partial void OnSkipMovie(RpcContext context, SkipMovieData data)
    {
        logger.LogDebug("Received skip movie request from player {PlayerId}, movie id {Id}", context.Sender, data.SequenceId);
        if (!_skipMovieRequests.TryGetValue(data.SequenceId, out var playerSet))
        {
            playerSet = [context.Sender];
            _skipMovieRequests[data.SequenceId] = playerSet;
        }
        else
        {
            playerSet.Add(context.Sender);
        }

        var connectedPlayers = 0;
        ecs.Query<MainCharacterComponent>((ref _) =>
        {
            connectedPlayers++;
        });

        var response = new SkipMovieData
        {
            SequenceId = data.SequenceId,
            WaitingPlayers = playerSet.Count,
            AllPlayers = connectedPlayers
        };

        if (response.WaitingPlayers == response.AllPlayers)
        {
            logger.LogInformation("Skipping movie {Id} as all players requested it", data.SequenceId);
            _skipMovieRequests.Remove(data.SequenceId);
        }

        foreach (var playerId in playerSet)
        {
            SendSkipMovie(playerId, response);
        }
    }

    // partial void OnMovieStarted(RpcContext context, int sequenceId, AreaId areaId)
    // {
    //     if (serverState.AreaEntries.TryGetValue(areaId, out var areaEntry))
    //     {
    //         var areaEntity = areaEntry.AreaEntity;
    //         ref var movieComp = ref areaEntity.GetComponent<MovieComponent>();
    //         movieComp.StartedSequences = movieComp.StartedSequences.Add(sequenceId);
    //         logger.LogDebug("Marked movie {Id} as started in area {AreaId}", sequenceId, areaId);
    //     }
    // }
    //
    // partial void OnMovieFinished(RpcContext context, int sequenceId, AreaId areaId)
    // {
    //     if (serverState.AreaEntries.TryGetValue(areaId, out var areaEntry))
    //     {
    //         var areaEntity = areaEntry.AreaEntity;
    //         ref var movieComp = ref areaEntity.GetComponent<MovieComponent>();
    //         movieComp.FinishedSequences = movieComp.FinishedSequences.Add(sequenceId);
    //         logger.LogDebug("Marked movie {Id} as finished in area {AreaId}", sequenceId, areaId);
    //     }
    // }
}