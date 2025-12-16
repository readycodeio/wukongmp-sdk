using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using b1;
using HarmonyLib;
using PreludeLib.Attributes;
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

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnEnterPreAnimationSyncingStateOnHost
{
    [HarmonyTargetMethodHint("b1.BUS_AnimationSyncHostComp", "OnEnterPreAnimationSyncingStateOnHost")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_AnimationSyncHostComp:OnEnterPreAnimationSyncingStateOnHost");
    }

    // Disable animation syncing attacks for monsters not owned by the local player
    public static void Postfix(UActorCompBaseCS __instance, AActor Guest, List<int> PreAnimationSyncStateHostBuffList)
    {
        var owner = __instance.GetOwner() as BGU_CharacterAI;
        var guest = Guest as BGUCharacterCS;

        if (owner != null && guest != null)
        {
            var hostEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (hostEntity.HasValue)
            {
                if (DI.Instance.ClientOwnership.OwnsEntity(hostEntity.Value.Entity))
                {
                    if (DI.Instance.PawnState.TryGetEntityByCharacter(guest, out var guestEntity))
                    {
                        var hostId = hostEntity.Value.GetMeta().NetId;
                        var guestId = guestEntity.Value.GetComponent<MetadataComponent>().NetId;
                        DI.Instance.Rpc.SendPreAnimationSyncing(new PreAnimationSyncingData(hostId, guestId));
                    }
                }
            }
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnEnterAnimationSyncingStateOnHost
{
    [HarmonyTargetMethodHint("b1.BUS_AnimationSyncHostComp", "OnEnterAnimationSyncingStateOnHost")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_AnimationSyncHostComp:OnEnterAnimationSyncingStateOnHost");
    }

    // Disable animation syncing attacks for monsters not owned by the local player
    public static void Postfix(UActorCompBaseCS __instance, List<int> AnimationSyncStateHostBuffList, UAnimMontage? AnimationSyncMontage)
    {
        var owner = __instance.GetOwner() as BGU_CharacterAI;

        if (owner != null)
        {
            var entity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (entity.HasValue)
            {
                if (DI.Instance.ClientOwnership.OwnsEntity(entity.Value.Entity))
                {
                    var shortened = Compressors.MontageNameCompressor.Compress(AnimationSyncMontage?.PathName, out var shortMontagePath);
                    var data = shortened ? shortMontagePath : AnimationSyncMontage?.PathName ?? "";
                    var evData = new MontageCallbackData(entity.Value.GetMeta().NetId, shortened, data, 0, true);
                    DI.Instance.Rpc.SendAnimationSyncing(evData);
                }
            }
        }
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