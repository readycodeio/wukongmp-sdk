using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongGameplaySettings(Store world, WukongRoomState roomState)
{
    public void SetMonsterHpScaling(int scaling)
    {
        if (!roomState.IsMasterClient)
        {
            UIUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        world.Query<HpComponent, LocalTamerComponent>().Each(new ScaleMonsterHpJob(scaling));
    }
}
