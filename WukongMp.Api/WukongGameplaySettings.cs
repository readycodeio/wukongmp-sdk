using Friflo.Engine.ECS;
using ReadyM.Api;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongGameplaySettings(Store world, RelayClient relayClient)
{
    public void SetMonsterHpScaling(int scaling)
    {
        if (!relayClient.IsMasterClient)
        {
            UIUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        world.Query<HpComponent, LocalTamerComponent>().Each(new ScaleMonsterHpJob(scaling));
    }
}
