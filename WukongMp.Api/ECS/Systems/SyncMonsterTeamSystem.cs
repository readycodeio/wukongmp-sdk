using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Old.Api;

namespace WukongMp.Api.ECS.Systems;

public class SyncMonsterTeamSystem : QuerySystem<TeamComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref team, ref localTamer, _) =>
        {
            if (!localTamer.IsMonsterSynced || localTamer.Pawn == null)
                return;

            if (team.TeamId != localTamer.Pawn.GetTeamIDInCS())
            {
                ClientUtils.RegisterNewPlayerTeam(localTamer.Pawn, team.TeamId);

            }
        });
    }
}