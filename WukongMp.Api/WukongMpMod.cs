using System;
using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using LiteNetLib.Utils;
using ReadyM.Api;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong;
using ReadyM.Relay.Common.Wukong.Components;
using ReadyM.Relay.Common.Wukong.Jobs;
using UnrealEngine.Engine;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;

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
            GameUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        World.Query<HpComponent, LocalTamerComponent>().Each(new ScaleMonsterHpJob(scaling));
    }
}