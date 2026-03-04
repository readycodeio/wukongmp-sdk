using Friflo.Engine.ECS.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public class SyncMonsterTeamSystem : QuerySystem<TeamComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref team, ref localTamer, entity) =>
        {
            var tamerEntity = new TamerEntity(entity);
            
            if (!localTamer.IsMonsterActive || tamerEntity.Pawn == null)
                return;

            if (team.TeamId != tamerEntity.Pawn.GetTeamIDInCS())
            {
                ClientUtils.RegisterAndSetPlayerTeam(tamerEntity.Pawn, team.TeamId);
            }
        });
    }
}