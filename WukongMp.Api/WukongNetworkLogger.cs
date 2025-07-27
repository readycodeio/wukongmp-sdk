using System;
using System.Collections.Generic;
using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Idents;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using WukongMp.Api.Old;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongNetworkLogger : IDisposable
{
    private readonly ILogger _logger;
    private readonly Store _world;
    private readonly WukongRoomState _roomState;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly IRelayClient _relayClient;

    public WukongNetworkLogger(ILogger logger, Store world, WukongRoomState roomState, WukongPlayerRegistry playerRegistry, IRelayClient relayClient)
    {
        _logger = logger;
        _world = world;
        _roomState = roomState;
        _playerRegistry = playerRegistry;
        _relayClient = relayClient;
        
        _relayClient.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
    }

    public void Dispose()
    {
        _relayClient.OnRoomPropertiesChanged -= OnRoomPropertiesChanged;
    }
    
    private void OnRoomPropertiesChanged(Dictionary<object, object?> diff)
    {
        if (diff.TryGetValue(RoomProperties.MasterClientId, out var id) && id is PlayerId newMasterId)
        {
            _logger.LogInformation("Master client changed to {NewMasterId}", newMasterId);
        }
    }
    
    public void DumpDebugInfo()
    {
        // dump room state
        Logging.LogDebug("Room state: {State}", _roomState.ToString());

        if (_playerRegistry.HasLocalPlayerState)
        {
            // dump player state to console for me
            Logging.LogDebug("Local player state: {State}", _playerRegistry.LocalPlayerState.ToString());
        }
        else
        {
            Logging.LogDebug("No local player state found.");
        }
        
        // dump player state to console for each connected player
        foreach (var (id, state) in _playerRegistry.ConnectedPlayers)
        {
            Logging.LogDebug("Player {PlayerId} state: {State}", id, state.ToString());
        }

        // dump synced monsters
        _world.Query<NetworkIdComponent>().ForEachEntity((ref netId, entity) =>
        {
            Logging.LogDebug("Monster {Entity}: {NetId}", entity, netId);
            // TODO: Dump all monster info without using .DebugJson (throws due to some internal errors,
            // probably the same reason why JsonSerializer sometimes fails.
        });

        // print team hostility info
        var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

        foreach (var (teamId, relation) in teamRelationData.TeamHostileInfos)
        {
            Logging.LogDebug("Team {TeamId} hostility: {HostileTeams}", teamId, string.Join(", ", relation.HostileTeamIDs));
        }

        // dump perf info
        var perf = _world.SystemRoot.GetPerfLog();
        if (perf != null)
        {
            Logging.LogDebug("Perf log:\n{Log}", perf);
        }
        else
        {
            Logging.LogDebug("Perf log is null");
        }
    }
}