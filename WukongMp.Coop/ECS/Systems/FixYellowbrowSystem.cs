using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop.ECS.Systems;

public class FixYellowbrowSystem(WukongAreaState areaState, WukongPlayerState playerState) : QuerySystem<TamerComponent, LocalTamerComponent, HpComponent>
{
    protected override void OnUpdate()
    {
        if (!areaState.InRoom || !playerState.LocalMainCharacter.HasValue)
            return;

        Query.ForEachEntity((ref tamer, ref localTamer, ref hp, entity) =>
        {
            if (localTamer.IsMonsterActive && hp.Hp < 1f && tamer.Guid == "UGuid.LYS.HuangMei.Big")
            {
                if (playerState.LocalMainCharacter.Value.GetState().IsDead)
                {
                    // rebirth player
                    playerState.LocalMainCharacter.Value.GetPvP().IsSpectator = false;
                    PlayerUtils.RebirthPlayerInPlace(playerState.LocalMainCharacter.Value.GetLocalState().Pawn);
                }
            }
        });
    }
}