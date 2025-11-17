using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public class SyncMonsterTeamSystem : QuerySystem<TeamComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref team, ref localTamer, entity) =>
        {
            if (!localTamer.IsMonsterActive || localTamer.Pawn == null)
                return;

            if (team.TeamId != localTamer.Pawn.GetTeamIDInCS())
            {
                ClientUtils.RegisterAndSetPlayerTeam(localTamer.Pawn, team.TeamId);
            }
        });
    }
}