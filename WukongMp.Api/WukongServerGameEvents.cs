using b1;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Mapping.Events;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

internal class WukongServerGameEvents(
    IMappedEventManager mappedEvent,
    WukongWidgetManager widgetManager,
    ILogger logger
) : IHostedService
{
    private readonly WukongWidgetManager _widgetManager = widgetManager;
    private readonly ILogger _logger = logger;

    public void OnScopeStart()
    {
        mappedEvent.RegisterGameEventHandler<SkipMovieEvent, WukongServerGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("Received skip movie event from server, sequence id: {Id}, waiting: {Waiting}/{All}", ev.SequenceId, ev.WaitingPlayers, ev.AllPlayers);

            if (ev.WaitingPlayers == ev.AllPlayers)
            {
                self._widgetManager.HideInfoMessage();
                CutsceneUtils.SkipCutscene(ev.SequenceId);
            }
            else
            {
                self._widgetManager.ShowInfoMessage(string.Format(BuiltinTexts.WaitForOtherPlayersCount, ev.WaitingPlayers, ev.AllPlayers));
            }
        }, this);

        mappedEvent.RegisterGameEventHandler<BeguilingChantEvent>(static ev =>
        {
            var areaActors = UGameplayStatics.GetAllActorsOfClass<BGUIntervalArea>(GameUtils.GetWorld());

            foreach (var area in areaActors)
            {
                var comp = area.GetComponent<BUS_IntervalTriggerImpl>();
                if (comp != null)
                {
                    var isActive = ev.State == BeguilingChantState.Active;
                    var isWarning = ev.State == BeguilingChantState.Warning;
                    AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "SetIsActive").Invoke(comp, [isActive]);

                    if (isWarning)
                    {
                        AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "CheckIsWarning").Invoke(comp, [0f]);
                    }
                    else
                    {
                        AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "ResetNotiedWarning").Invoke(comp, []);
                    }
                }
            }
        });
    }

    public void Dispose()
    {
        // FIXME: Unregister event handlers
    }
}