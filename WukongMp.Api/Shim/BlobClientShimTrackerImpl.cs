using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Common.Shim;

namespace WukongMp.Api.Shim;

public class BlobClientShimTrackerImpl : IShimDependencyTrackerImpl
{
    public bool Supports(ShimRequestItem requestItem)
    {
        throw new System.NotImplementedException();
    }
    public bool Supports(ShimResponseItem responseItem)
    {
        throw new System.NotImplementedException();
    }
    public bool CheckRequestHasResponse(ShimRequestItem requestItem, ShimResponseItem responseItem)
    {
        throw new System.NotImplementedException();
    }
    public bool CheckResponseShouldWait(ShimResponseItem responseItem, IRelayClientNetworkThreadContext context, IEnumerable<ShimRequestItem> requestItems)
    {
        throw new System.NotImplementedException();
    }
}