using b1;
using HarmonyLib;
using ReadyM.Api.Multiplayer.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.NameCompressors;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGS_AnimationSyncSystem), "OnBeginAnimationSyncPreCheck")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnBeginAnimationSyncPreCheck
{
    // Disable animation syncing attacks for monsters not owned by the local player
    public static bool Prefix(BGS_AnimationSyncSystem __instance)
    {
        var owner = __instance.GetOwner() as BGU_CharacterAI;

        if (owner != null)
        {
            var entity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (entity.HasValue)
            {
                return DI.Instance.ClientOwnership.OwnsEntity(entity.Value.Entity);
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(BGS_AnimationSyncSystem), "OnPreCheckSuccess")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnPreCheckSuccess
{
    // Disable animation syncing attacks for monsters not owned by the local player
    public static void Postfix(AActor Host, AActor GuestCandidate, UAnimMontage? AnimationSyncMontage)
    {
        if (!DI.Instance.PawnState.TryGetEntityByCharacter(Host as BGUCharacterCS, out var hostEntity))
            return;

        if (!DI.Instance.ClientOwnership.OwnsEntity(hostEntity.Value))
            return;

        if (!DI.Instance.PawnState.TryGetEntityByCharacter(GuestCandidate as BGUCharacterCS, out var guestEntity))
            return;

        var hostId = hostEntity.Value.GetComponent<MetadataComponent>().NetId;
        var guestId = guestEntity.Value.GetComponent<MetadataComponent>().NetId;

        var shortened = Compressors.MontageNameCompressor.Compress(AnimationSyncMontage?.PathName, out var shortMontagePath);
        var montage = shortened ? shortMontagePath : AnimationSyncMontage?.PathName ?? "";

        var payload = new AnimationSyncingData(hostId, guestId, shortened, montage);
        DI.Instance.Rpc.SendAnimationSyncing(payload);
    }
}

[HarmonyPatch(typeof(BGS_AnimationSyncSystem), "OnBeginSyncAnimation")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnBeginSyncAnimation
{
    // Disable animation syncing attacks for monsters not owned by the local player
    public static void Postfix(AActor Host,
        UAnimMontage? GuestMontage,
        bool bFoundHostSyncPointOnDummyMesh,
        FName SelfSyncPointOnHost,
        FName TargetSyncPointOnHost,
        FName SelfSyncPointOnGuest,
        bool bForceSyncDummyMeshAnimation,
        bool bEnableDebugDraw,
        float NotifyBeginTime,
        float TotalDuration,
        int AnimationSyncMontageInstanceID)
    {
        var owner = Host as BGU_CharacterAI;
        if (owner != null)
        {
            var entity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (entity.HasValue)
            {
                if (DI.Instance.ClientOwnership.OwnsEntity(entity.Value.Entity))
                {
                    var shortened = Compressors.MontageNameCompressor.Compress(GuestMontage?.PathName, out var shortMontagePath);
                    var montage = shortened ? shortMontagePath : GuestMontage?.PathName ?? "";
                    var data = new BeginSyncAnimationData
                    {
                        Host = entity.Value.GetMeta().NetId,
                        Shortened = shortened,
                        GuestMontage = montage,
                        AnimationSyncMontageInstanceId = AnimationSyncMontageInstanceID,
                        bEnableDebugDraw = bEnableDebugDraw,
                        bForceSyncDummyMeshAnimation = bForceSyncDummyMeshAnimation,
                        bFoundHostSyncPointOnDummyMesh = bFoundHostSyncPointOnDummyMesh,
                        NotifyBeginTime = NotifyBeginTime,
                        SelfSyncPointOnGuest = SelfSyncPointOnGuest.ToString(),
                        SelfSyncPointOnHost = SelfSyncPointOnHost.ToString(),
                        TargetSyncPointOnHost = TargetSyncPointOnHost.ToString(),
                        TotalDuration = TotalDuration
                    };
                    DI.Instance.Rpc.SendBeginSyncAnimation(data);
                }
            }
        }
    }
}