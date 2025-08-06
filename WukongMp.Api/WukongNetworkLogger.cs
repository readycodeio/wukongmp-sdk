using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Compat;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongNetworkLogger(
    ILogger logger,
    Store world,
    ClientState state,
    WukongAreaState areaState,
    WukongPlayerState playerState)
{
    public void DumpDebugInfo()
    {
        // dump room state
        logger.LogDebug("Room state: {State}", areaState.ToString());

        if (playerState.LocalPlayer != null)
        {
            // dump player state to console for me
            logger.LogDebug("Local player state: {State}", playerState.LocalPlayer.Value.GetState());
        }
        else
        {
            logger.LogDebug("No local player state found.");
        }
        
        // dump player state to console for each connected player
        foreach (var playerId in state.AllPlayers)
        {
            var playerEntity = playerState.GetPlayerById(playerId);
            logger.LogDebug("Player {PlayerId} state: {State}", playerId, playerEntity.ToString());
        }

        // dump synced monsters
        world.Query<MetadataComponent>().ForEachEntity((ref meta, entity) =>
        {
            logger.LogDebug("Monster {Entity}: {NetId}", entity, meta.NetId);
            // TODO: Dump all monster info without using .DebugJson (throws due to some internal errors,
            // probably the same reason why JsonSerializer sometimes fails.
        });

        // print team hostility info
        var teamRelationData = (BGC_TeamRelationData)BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld());

        foreach (var (teamId, relation) in teamRelationData.TeamHostileInfos)
        {
            logger.LogDebug("Team {TeamId} hostility: {HostileTeams}", teamId, string.Join(", ", relation.HostileTeamIDs));
        }

        // dump perf info
        var perf = world.SystemRoot.GetPerfLog();
        if (perf != null)
        {
            logger.LogDebug("Perf log:\n{Log}", perf);
        }
        else
        {
            logger.LogDebug("Perf log is null");
        }
    }
}