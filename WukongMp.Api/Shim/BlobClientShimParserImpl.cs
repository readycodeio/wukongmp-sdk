using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Client.Shim;

namespace WukongMp.Api.Shim;

public class BlobClientShimParserImpl : IShimRelayMessageParserImpl
{
    public bool SupportsRequest(ServerEventHeader header)
    {
        throw new System.NotImplementedException();
    }
    public bool SupportsRequest(CustomRelayEventHeader header)
    {
        throw new System.NotImplementedException();
    }
    public bool SupportsResponse(ServerEventHeader header)
    {
        throw new System.NotImplementedException();
    }
    public bool SupportsResponse(CustomRelayEventHeader header)
    {
        throw new System.NotImplementedException();
    }
    public object? GetBuiltInRequestCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
    {
        throw new System.NotImplementedException();
    }
    public object? GetServerRpcRequestCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
    {
        throw new System.NotImplementedException();
    }
    public object? GetClientRpcRequestCustomDataUntyped(CustomRelayEventHeader header, NetDataReader reader)
    {
        throw new System.NotImplementedException();
    }
    public object? GetBuiltInResponseCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
    {
        throw new System.NotImplementedException();
    }
    public object? GetServerRpcResponseCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
    {
        throw new System.NotImplementedException();
    }
    public object? GetClientRpcResponseCustomDataUntyped(CustomRelayEventHeader header, NetDataReader reader)
    {
        throw new System.NotImplementedException();
    }
}