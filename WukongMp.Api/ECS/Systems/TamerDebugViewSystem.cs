using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems;

public class TamerDebugViewSystem(WukongEventBus eventBus, WukongWidgetManager widgetManager) : QuerySystem<LocalTamerComponent, TamerComponent, TranslationComponent>
{
    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong tickCounter;
    private bool _isAdded = false;

    protected override void OnUpdate()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (!widgetManager.IsDebugViewVisible)
            return;

        if (tickCounter++ % TickInterval != 0)
            return;

        Query.ForEachEntity((ref localTamer, ref tamer, ref translation, entity) =>
        {
            if(!localTamer.IsMonsterActive)
                return;

            if (tamer.Guid == null)
                return;

            if (!tamer.Guid.Contains("JiRuHuo"))
                return;

            if (!_isAdded)
            {
                widgetManager.AddCharacterToDebugView("JiRuHuo");
                _isAdded = true;
            }
            var position = localTamer.Pawn?.GetActorLocation() ?? FVector.ZeroVector;
            var ecsPosition = translation.Position.ToFVector();

            widgetManager.UpdatePlayerPosition("JiRuHuo", position, ecsPosition);
        });
    }
}
