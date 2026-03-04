using System;
using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Shim;
using ReadyM.Relay.Client.Shim;

namespace WukongMp.Api.Shim;

public class BlobClientShimTrackerImpl : IShimDependencyTrackerImpl
{
    public bool Supports(ShimRequestItem requestItem)
    {
        throw new NotImplementedException();
    }
    public bool Supports(ShimResponseItem responseItem)
    {
        throw new NotImplementedException();
    }
    public bool CheckRequestHasResponse(ShimRequestItem requestItem, ShimResponseItem responseItem)
    {
        throw new NotImplementedException();
    }
    public bool CheckResponseShouldWait(ShimResponseItem responseItem, IRelayClientNetworkThreadContext context, IEnumerable<ShimRequestItem> requestItems)
    {
        throw new NotImplementedException();
    }
}