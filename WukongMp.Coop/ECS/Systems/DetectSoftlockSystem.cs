using System;
using System.Collections.Generic;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Coop.ECS.Systems;

public class DetectSoftlockSystem(
    WukongAreaState areaState,
    WukongPlayerState playerState,
    WukongWidgetManager widgetManager,
    ILogger logger
) : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        if (!areaState.IsMasterClient)
            return;

        var players = 0;
        HashSet<int> waitingSequencesIds = new();

        Query.ForEachEntity((ref localMainComp, ref mainComp, _) =>
        {
            if (!localMainComp.HasPawn)
                return;

            players++;

            if (localMainComp.IsWaitingForSequence)
                waitingSequencesIds.Add(mainComp.WaitingSequenceId);
        });

        if (players == 0)
            return;

        var mainEntity = playerState.LocalMainCharacter;
        if (!mainEntity.HasValue)
        {
            logger.LogWarning("Skipping respawn, no local main character entity");
            return;
        }

        ref var localMainComp = ref mainEntity.Value.GetLocalState();

        if (players > 0 && waitingSequencesIds.Count > 1 && !localMainComp.IsRespawning)
        {
            logger.LogDebug("Softlock detected");
            widgetManager.ShowInfoMessage(Texts.SoftlockDetected);
        }
    }
}
