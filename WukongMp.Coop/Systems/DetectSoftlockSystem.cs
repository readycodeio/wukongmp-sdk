using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using WukongMp.Api.Resources;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

public sealed class DetectSoftlockSystem(WukongLocalApi localApi, WukongClientApi clientApi, ILogger logger)
    : PluginSystemBase(localApi, clientApi, logger)
{
    private readonly HashSet<int> _waitingSequencesIds = [];
    
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!ClientApi.IsMasterClient)
            return;

        var players = 0;
        _waitingSequencesIds.Clear();

        foreach (var mainCharacter in ClientApi.AllMainCharacters)
        {
            if (mainCharacter.AreaId != ClientApi.CurrentAreaId)
                continue;

            players++;

            if (mainCharacter.IsWaitingForSequence)
            {
                _waitingSequencesIds.Add(mainCharacter.WaitingSequenceId);
            }
        }

        if (players == 0)
            return;

        var localMainCharacter = ClientApi.LocalMainCharacter;
        if (!localMainCharacter.HasValue)
        {
            Logger.LogWarning("Skipping respawn, no local main character entity");
            return;
        }

        if (players > 0 && _waitingSequencesIds.Count > 1 && !localMainCharacter.Value.IsRespawning)
        {
            Logger.LogDebug("Softlock detected");
            LocalApi.ShowInfoMessage(Texts.SoftlockDetected);
        }
    }
}
