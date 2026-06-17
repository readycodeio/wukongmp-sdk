using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;

namespace ReadyM.Wukong.Common.Rpc;

[ServerRpcContracts]
public static partial class RpcContracts
{
    public static partial void Ping(long timestamp);
    public static partial void SkipMovie(SkipMovieData data);
    public static partial void MovieStarted(int sequenceId, AreaId areaId);
    public static partial void MovieFinished(int sequenceId, AreaId areaId);
}