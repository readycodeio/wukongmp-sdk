using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems;

public class DebugViewSystem(
    WukongEventBus eventBus, 
    WukongWidgetManager widgetManager) : QuerySystem<MainCharacterComponent>
{
    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong tickCounter;

    protected override void OnUpdate()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (!widgetManager.IsDebugViewVisible)
            return;

        if (tickCounter++ % TickInterval != 0)
            return;

        Query.ForEachEntity((ref mainComp, entity) =>
        {
            var mainEntity = new MainCharacterEntity(entity);
            var pawn = mainEntity.Pawn;
            var nickname = mainComp.CharacterNickName;
            var position = pawn?.GetActorLocation() ?? FVector.ZeroVector;
            var ecsPosition = mainComp.Location.ToFVector();

            widgetManager.UpdatePlayerPosition(nickname, position, ecsPosition);
        });
    }
}