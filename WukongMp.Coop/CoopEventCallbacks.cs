using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop;

public sealed class CoopEventCallbacks(ILogger logger) : IHostedService, IDisposable
{
    public void OnScopeStart()
    {
        WukongApi.Events.OnJoinedArea += OnJoinedAreaHandler;
        WukongApi.Events.OnPlayerPawnSpawned += OnPlayerPawnSpawned;
        WukongApi.Events.OnMainCharacterEntityInitialized += OnMainCharacterEntityInitialized;
    }

    public void Dispose()
    {
        WukongApi.Events.OnJoinedArea -= OnJoinedAreaHandler;
        WukongApi.Events.OnPlayerPawnSpawned -= OnPlayerPawnSpawned;
        WukongApi.Events.OnMainCharacterEntityInitialized -= OnMainCharacterEntityInitialized;
    }

    private static void OnPlayerPawnSpawned(ReadyMainCharacter player)
    {
        const string whiteColor = "(R=0.9,G=0.9,B=0.9)";
        player.SetMarkerMessage(player.Nickname, whiteColor);
    }

    private static void OnMainCharacterEntityInitialized(ReadyMainCharacter player)
    {
        // check if we are in the Pagoda
        var areaActors = UGameplayStatics.GetAllActorsOfClass<BGUIntervalArea>(GameUtils.GetWorld());
        foreach (var area in areaActors)
        {
            var comp = area.GetComponent<BUS_IntervalTriggerImpl>();
            if (comp != null)
            {
                var eligible = comp.CurrentState is BUS_IntervalTriggerImpl.IntervalTriggerEnableState;
                player.BeguilingChantEligible = eligible;
                return;
            }
        }
    }

    private void OnJoinedAreaHandler(AreaId areaId)
    {
        var isFirst = WukongApi.Sync.IsMasterClient;
        logger.LogInformation("Joined area {AreaId}, is master client: {IsMasterClient}", areaId, isFirst);

        if (isFirst)
        {
            // it's enough for 1 player to sync the monsters in the area
            WukongApi.Sync.SyncMonstersInArea();
        }
    }
}