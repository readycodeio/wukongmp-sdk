using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public partial class WukongMpMod : WukongMpModBase
{
    public static WukongMpMod Instance { get; } = new();

    private WukongMpMod() { }

    protected override void OnPingUpdated(int ping)
    {
        PingIndicatorWidget.Instance.SetPingValue(ping);
    }

    // TODO: After we remove the Client, this will be removed as well, leaving just the call to Tick()
    public void RunEcsWorldUpdate()
    {
        Client.SetCachedPlayerProperties();
        Tick(default);
    }

    public void SetMonsterHpScaling(int scaling)
    {
        if (!IsMasterClient)
        {
            UIUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        World.Query<HpComponent, LocalTamerComponent>().Each(new ScaleMonsterHpJob(scaling));
    }
}