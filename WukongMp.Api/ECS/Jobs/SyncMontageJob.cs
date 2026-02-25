using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.ECS.Jobs;

public readonly struct SyncMontageJob(Store world, PlayerId ownerPlayerId) : IEachEntity<MappingComponent<AActor>, LocalTamerComponent, MetadataComponent>
{
    public void Execute(ref MappingComponent<AActor> mappingComp, ref LocalTamerComponent tamerComponent, ref MetadataComponent meta, int entityId)
    {
        if (meta.Owner != ownerPlayerId)
            return;

        var entity = new TamerEntity(world.GetEntityById(entityId));
        var pawn = entity.Pawn;

        if (pawn == null || pawn.Mesh == null)
            return;

        var montageState = tamerComponent.MontageState;
        if (montageState.LocalAnimationInstance == null)
        {
            montageState.LocalAnimationInstance = pawn.Mesh.GetAnimInstance();
            if (montageState.LocalAnimationInstance == null)
                return;
        }

        var currentMontage = pawn.GetCurrentMontage();

        if (currentMontage != null)
        {
            bool isNewMontage = montageState.LocalMontage != currentMontage;
            float currentPosition = montageState.LocalAnimationInstance.Montage_GetPosition(currentMontage);

            bool hasMontageRewound = currentPosition < montageState.LocalMontagePosition && !isNewMontage;
            bool hasSkippedFrames = currentPosition - montageState.LocalMontagePosition > 0.5f && !isNewMontage;

            if (isNewMontage || hasMontageRewound || hasSkippedFrames)
            {
                // TODO: Replace by system
                DI.Instance.ClientRpc.SendMontageCallback(meta.NetId, currentMontage, currentPosition, hasMontageRewound);
            }

            montageState.LocalMontagePosition = currentPosition;
        }
        else if (montageState.LocalMontage != null)
        {
            DI.Instance.Logger.LogDebug("Sent cancel at {Position} for montage {Montage}", montageState.LocalMontagePosition, montageState.LocalMontage.PathName);
            DI.Instance.ClientRpc.SendMontageCancel(meta.NetId);
        }

        montageState.LocalMontage = currentMontage;
        tamerComponent.MontageState = montageState;
    }
}