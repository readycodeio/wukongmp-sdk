using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace WukongMp.Api.ECS.Jobs;

public readonly struct SyncMontageJob(WukongRpcCallbacks rpc, PlayerId ownerPlayer) : IEach<LocalTamerComponent, MetadataComponent>
{
    public void Execute(ref LocalTamerComponent tamerComponent, ref MetadataComponent meta)
    {
        if (meta.Owner != ownerPlayer)
            return;

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
                rpc.SendMontageCallback(meta.NetId, currentMontage, currentPosition, hasMontageRewound);
            }

            montageState.LocalMontagePosition = currentPosition;
        }
        else if (montageState.LocalMontage != null)
        {
            DI.Instance.Rpc.SendMontageCancel(meta.NetId);
        }

        montageState.LocalMontage = currentMontage;
        tamerComponent.MontageState = montageState;
    }
}