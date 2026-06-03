using ReadyM.Relay.Server.Sdk;

namespace WukongMp.Sdk.Serverside;

public partial class RpcHandlers(RpcApi rpc) : ServerRpcHandlersBase(rpc)
{
    partial void OnPing(RpcContext context, long timestamp)
    {
        SendPing(context.Sender, timestamp);
    }
}