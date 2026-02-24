using System;
using Friflo.Engine.ECS;

namespace WukongMp.Api.ECS.GameEvents;

// NOTE(api): This seems unused. The original implementation never calls SendBeginAnimation
public readonly struct BeginSyncAnimationEvent(
    Entity host,
    string fullGuestMontage,
    bool foundHostSyncPointOnDummyMesh,
    string selfSyncPointOnHost,
    string targetSyncPointOnHost,
    string selfSyncPointOnGuest,
    bool forceSyncDummyMeshAnimation,
    bool enableDebugDraw,
    float notifyBeginTime,
    float totalDuration,
    int animationSyncMontageInstanceId) : IEquatable<BeginSyncAnimationEvent>
{
    public readonly Entity Host = host;
    public readonly string FullGuestMontage = fullGuestMontage;
    public readonly bool FoundHostSyncPointOnDummyMesh = foundHostSyncPointOnDummyMesh;
    public readonly string SelfSyncPointOnHost = selfSyncPointOnHost;
    public readonly string TargetSyncPointOnHost = targetSyncPointOnHost;
    public readonly string SelfSyncPointOnGuest = selfSyncPointOnGuest;
    public readonly bool ForceSyncDummyMeshAnimation = forceSyncDummyMeshAnimation;
    public readonly bool EnableDebugDraw = enableDebugDraw;
    public readonly float NotifyBeginTime = notifyBeginTime;
    public readonly float TotalDuration = totalDuration;
    public readonly int AnimationSyncMontageInstanceId = animationSyncMontageInstanceId;

    public bool Equals(BeginSyncAnimationEvent other)
        => (
            Host.Equals(other.Host) && 
            FullGuestMontage == other.FullGuestMontage && 
            FoundHostSyncPointOnDummyMesh == other.FoundHostSyncPointOnDummyMesh && 
            SelfSyncPointOnHost == other.SelfSyncPointOnHost && 
            TargetSyncPointOnHost == other.TargetSyncPointOnHost && 
            SelfSyncPointOnGuest == other.SelfSyncPointOnGuest && 
            ForceSyncDummyMeshAnimation == other.ForceSyncDummyMeshAnimation && 
            EnableDebugDraw == other.EnableDebugDraw && 
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            NotifyBeginTime == other.NotifyBeginTime && 
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            TotalDuration == other.TotalDuration && 
            AnimationSyncMontageInstanceId == other.AnimationSyncMontageInstanceId
        );

    public override bool Equals(object? obj)
        => obj is BeginSyncAnimationEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Host.GetHashCode();
            hashCode = (hashCode * 397) ^ FullGuestMontage.GetHashCode();
            hashCode = (hashCode * 397) ^ FoundHostSyncPointOnDummyMesh.GetHashCode();
            hashCode = (hashCode * 397) ^ SelfSyncPointOnHost.GetHashCode();
            hashCode = (hashCode * 397) ^ TargetSyncPointOnHost.GetHashCode();
            hashCode = (hashCode * 397) ^ SelfSyncPointOnGuest.GetHashCode();
            hashCode = (hashCode * 397) ^ ForceSyncDummyMeshAnimation.GetHashCode();
            hashCode = (hashCode * 397) ^ EnableDebugDraw.GetHashCode();
            hashCode = (hashCode * 397) ^ NotifyBeginTime.GetHashCode();
            hashCode = (hashCode * 397) ^ TotalDuration.GetHashCode();
            hashCode = (hashCode * 397) ^ AnimationSyncMontageInstanceId;
            return hashCode;
        }
    }
}