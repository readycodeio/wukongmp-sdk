using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct BeginSyncAnimationData(
    NetworkId hostNetId,
    bool compressed,
    string guestMontage,
    bool bFoundHostSyncPointOnDummyMesh,
    string selfSyncPointOnHost,
    string targetSyncPointOnHost,
    string selfSyncPointOnGuest,
    bool bForceSyncDummyMeshAnimation,
    bool bEnableDebugDraw,
    float notifyBeginTime,
    float totalDuration,
    int animationSyncMontageInstanceId) : INetSerializable
{
    public NetworkId HostNetId = hostNetId;
    public bool compressed = compressed;
    public string GuestMontage = guestMontage;
    public bool bFoundHostSyncPointOnDummyMesh = bFoundHostSyncPointOnDummyMesh;
    public string SelfSyncPointOnHost = selfSyncPointOnHost;
    public string TargetSyncPointOnHost = targetSyncPointOnHost;
    public string SelfSyncPointOnGuest = selfSyncPointOnGuest;
    public bool bForceSyncDummyMeshAnimation = bForceSyncDummyMeshAnimation;
    public bool bEnableDebugDraw = bEnableDebugDraw;
    public float NotifyBeginTime = notifyBeginTime;
    public float TotalDuration = totalDuration;
    public int AnimationSyncMontageInstanceId = animationSyncMontageInstanceId;
}