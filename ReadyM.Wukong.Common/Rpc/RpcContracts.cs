using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;

namespace ReadyM.Wukong.Common.Rpc;

[ServerRpcContracts]
public static partial class RpcContracts
{
    [ClientToServer, ServerToClient] public static partial void Ping(long timestamp);
    [ClientToServer, ServerToClient] public static partial void SkipMovie(SkipMovieData data);
    [ClientToServer, ServerToClient] public static partial void MovieStarted(int sequenceId, AreaId areaId);
    [ClientToServer, ServerToClient] public static partial void MovieFinished(int sequenceId, AreaId areaId);
}