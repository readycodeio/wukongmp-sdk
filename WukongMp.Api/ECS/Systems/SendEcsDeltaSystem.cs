using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Systems;

namespace WukongMp.Api.ECS.Systems;

public sealed class SendEcsDeltaSystem(RelayClient client) : SendEcsDeltaSystemBase
{
    protected override int GetMaxPacketSize()
    {
        return client.GetMaxPacketSize(DeliveryMethod.Unreliable);
    }

    protected override void Send(NetDataWriter data)
    {
        client.OpRaiseEventRaw(data, DeliveryMethod.Unreliable);
    }
    
    protected override bool OwnsEntity(NetworkIdComponent netId)
    {
        return netId.Creator == client.PlayerId;
    }
}