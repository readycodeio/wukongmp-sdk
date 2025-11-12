using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.ECS.Components;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP.ECS.Systems;

public class TeamColorSystem : QuerySystem<MainCharacterComponent, LocalMainCharacterComponent, TeamComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref mainComp, ref localMainComp, ref teamComp, _) =>
        {
            if (!localMainComp.HasPawn)
                return;

            if (localMainComp.Pawn!.GetTeamIDInCS() != teamComp.TeamId)
            {
                Logging.LogDebug("Updating player {Nickname} to team {Team}", mainComp.CharacterNickName, teamComp.TeamId);

                if (localMainComp.MarkerActor != null)
                {
                    var teamColor = PvpUtils.GetTeamColorString(teamComp.TeamId);
                    localMainComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {mainComp.CharacterNickName} {teamColor}", true);
                }
            }
        });
    }
}