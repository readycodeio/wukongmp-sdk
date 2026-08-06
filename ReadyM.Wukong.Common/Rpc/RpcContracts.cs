using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;

namespace ReadyM.Wukong.Common.Rpc;

[ServerRpcContracts]
public static partial class RpcContracts
{
    [ClientToServer, ServerToClient] public static partial void Ping(long timestamp);
    [ClientToServer] public static partial void EnableCheats(AreaId area, bool enabled);
    [ClientToServer, ServerToClient] public static partial void SkipMovie(SkipMovieData data);
    [ClientToServer] public static partial void MovieStarted(int sequenceId, AreaId areaId);
    [ClientToServer] public static partial void MovieFinished(int sequenceId, AreaId areaId);
}