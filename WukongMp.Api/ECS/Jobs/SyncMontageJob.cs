using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Mapping.Events;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Mapping;

namespace WukongMp.Api.ECS.Jobs;

public readonly struct SyncMontageJob(WukongMappingPolicyDirectory policyDir, IMappedEventManager mappedEvent, ILogger logger)
{
    // TODO: Replace by system
    public void Execute(ref LocalTamerComponent localTamerComp, Entity entity)
    {
        var tamerEntity = new TamerEntity(entity);

        var pawn = tamerEntity.Pawn;
        if (pawn == null || pawn.Mesh == null)
            return;

        var montageState = localTamerComp.MontageState;
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
                if (policyDir.TamerEvent<MontageCallbackEvent>().ShouldEventPropagateToEcs(tamerEntity))
                {
                    mappedEvent.PropagateToEcs(new MontageCallbackEvent(
                        entity: tamerEntity.Entity,
                        fullMontagePath: currentMontage.PathName,
                        position: currentPosition,
                        reset: hasMontageRewound
                    ));
                }
            }

            montageState.LocalMontagePosition = currentPosition;
        }
        else if (montageState.LocalMontage != null)
        {
            logger.LogDebug("Sent cancel at {Position} for montage {Montage}", montageState.LocalMontagePosition, montageState.LocalMontage.PathName);

            if (policyDir.TamerEvent<MontageCancelEvent>().ShouldEventPropagateToEcs(tamerEntity))
            {
                mappedEvent.PropagateToEcs(new MontageCancelEvent(tamerEntity.Entity));
            }
        }

        montageState.LocalMontage = currentMontage;
        localTamerComp.MontageState = montageState;
    }
}