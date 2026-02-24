using System.Diagnostics;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Mapping;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public class SyncMonsterTeamSystem(WukongMappingPolicyDirectory policyDir) 
    : QuerySystem<TeamComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref teamComp, ref _, entity) =>
        {
            var tamerEntity = new TamerEntity(entity);
            var pawn = tamerEntity.Pawn;

            if (pawn == null)
                return;

            if (teamComp.TeamId != pawn.GetTeamIDInCS())
            {
                if (policyDir.TamerData<TeamComponent>().ShouldEcsCopyToGame(tamerEntity))
                {
                    ClientUtils.RegisterAndSetPlayerTeam(pawn, teamComp.TeamId);
                }
                else
                {
                    // NOTE(api): API refactoring only
                    Debug.Assert(false);
                }
            }
        });
    }
}