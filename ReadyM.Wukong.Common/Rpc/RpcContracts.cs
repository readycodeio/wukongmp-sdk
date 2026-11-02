using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.SDK.Attributes;

namespace ReadyM.Wukong.Common.Rpc;

/// <exclude />
[RpcContracts]
public static partial class SdkRpcContracts
{
    [ClientToServer]
    public static partial void SkipMovie(int sequenceId);

    [ServerToClient]
    public static partial void SkipMovie(SkipMovieData data);

    [ClientToServer]
    public static partial void MovieStarted(int sequenceId, AreaId areaId);
}