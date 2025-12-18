using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Jobs;

public readonly struct SyncMontageJob(WukongRpcCallbacks rpc, PlayerId ownerPlayerId) : IEach<LocalTamerComponent, MetadataComponent>
{
    public void Execute(ref LocalTamerComponent tamerComponent, ref MetadataComponent meta)
    {
        if (meta.Owner != ownerPlayerId)
            return;

        if (tamerComponent.Pawn == null || tamerComponent.Pawn.Mesh == null)
            return;

        var montageState = tamerComponent.MontageState;
        if (montageState.LocalAnimationInstance == null)
        {
            montageState.LocalAnimationInstance = tamerComponent.Pawn.Mesh.GetAnimInstance();
            if (montageState.LocalAnimationInstance == null)
                return;
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
            DI.Instance.Logger.LogDebug("Sent cancel at {Position} for montage {Montage}", montageState.LocalMontagePosition, montageState.LocalMontage.PathName);
            DI.Instance.Rpc.SendMontageCancel(meta.NetId);
        }

        montageState.LocalMontage = currentMontage;
        tamerComponent.MontageState = montageState;
    }
}