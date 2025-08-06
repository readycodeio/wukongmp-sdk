using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongGameplaySettings(Store world, WukongAreaState areaState)
{
    public void SetMonsterHpScaling(int scaling)
    {
        if (!areaState.IsMasterClient)
        {
            UIUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        world.Query<HpComponent, LocalTamerComponent>().Each(new ScaleMonsterHpJob(scaling));
    }
}
