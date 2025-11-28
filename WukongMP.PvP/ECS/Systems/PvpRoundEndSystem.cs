using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.State;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Gamemode;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP.ECS.Systems;

internal sealed class PvpRoundEndSystem(
    Store world,
    WukongAreaState areaState,
    PvpMode pvpMode,
    IClientEcsUpdateLoop ecsLoop
) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        if (!areaState.CurrentArea.HasValue || !areaState.OwnsPvpState)
            return;

        if (areaState.PvpState is not { InPvP: true })
            return;

        if (pvpMode.IsRoundEnding || pvpMode.PendingDaShengSecondPhaseSpawns > 0)
            return;

        // check if all players but one are dead
        var playerEntities = pvpMode.AllPvPPlayers.ToList();
        var aliveTeamIds = playerEntities.Where(p =>
            {
                var state = p.Character.GetState();
                var pvp = p.Character.GetPvP();
                return !pvp.IsSpectator && (!state.IsDead || state.IsTransformed);
            })
            .Select(x => x.Player.GetState().TeamId)
            .ToList();

        var aliveMonsters = new List<int>();
        world.Query<HpComponent, TeamComponent>().ForEachEntity((ref hpComp, ref teamComp, _) =>
        {
            if (hpComp.IsDead || !PvpConstants.CompetingTeamIds.Contains(teamComp.TeamId))
                return;

            aliveMonsters.Add(teamComp.TeamId);
        });

        var alivePlayersTeams = aliveTeamIds.Concat(aliveMonsters).ToList();

        var aliveTeamCount = alivePlayersTeams.Distinct().Count();

        var aliveTeamPlayers = alivePlayersTeams
            .GroupBy(teamId => teamId)
            .Select(group => new { TeamId = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count).ToList();

        if (aliveTeamIds.Count == 0)
        {
            Logging.LogInformation("All players are dead, ending round");
            var aliveTeamId = aliveTeamPlayers.Count > 0 ? aliveTeamPlayers[0].TeamId : PvpConstants.DrawTeamId;
            if (alivePlayersTeams.Count == 0)
            {
                Task.Run(async () => await pvpMode.EndRoundAsync(PvpUtils.GetOppositeTeam(aliveTeamId)));
            }
            else
            {
                Task.Run(async () => await pvpMode.EndRoundAsync(aliveTeamId));
            }

            return;
        }

        if (aliveTeamCount == 1)
        {
            Logging.LogInformation("One team with alive players, ending round");
            var winner = playerEntities.First(p => !p.Character.GetState().IsDead);
            ecsLoop.Scheduler.ScheduleFunc(async (_, pvp, winner0) => { await pvp.EndRoundAsync(winner0.Player.GetState().TeamId); }, pvpMode, winner);
        }
    }
}