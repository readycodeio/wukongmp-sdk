using Friflo.Engine.ECS;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.ECS.Components;

namespace WukongMp.Api.ECS.Jobs;

public sealed class SyncMontageJob
{
    public void Execute(EntityStore world)
    {
        world.Query<LocalTamerComponent, NetworkIdComponent>().ForEachEntity((ref tamerComponent, ref netId, _) =>
        {
            if (tamerComponent.Pawn == null)
                return;

            var montageState = tamerComponent.MontageState;
            if (montageState.LocalAnimationInstance == null)
            {
                montageState.LocalAnimationInstance = tamerComponent.Pawn.Mesh.GetAnimInstance();
            }

            var currentMontage = tamerComponent.Pawn.GetCurrentMontage();

            if (currentMontage != null)
            {
                bool isNewMontage = montageState.LocalMontage != currentMontage;
                float currentPosition = montageState.LocalAnimationInstance.Montage_GetPosition(currentMontage);

                bool hasMontageRewound = currentPosition < montageState.LocalMontagePosition && !isNewMontage;
                bool hasSkippedFrames = currentPosition - montageState.LocalMontagePosition > 0.5f && !isNewMontage;

                if (isNewMontage || hasMontageRewound || hasSkippedFrames)
                {
                    // TODO: Replace by system
                    WukongMP.Instance.Client.SendMontageCallback(netId, currentMontage, currentPosition, hasMontageRewound);
                }

                montageState.LocalMontagePosition = currentPosition;
            }
            else if (montageState.LocalMontage != null)
            {
                WukongMP.Instance.Client.SendMontageCancel(netId);
            }

            montageState.LocalMontage = currentMontage;
            tamerComponent.MontageState = montageState;
        });
    }
}