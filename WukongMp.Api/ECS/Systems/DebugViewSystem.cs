using Friflo.Engine.ECS.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems;

internal class DebugViewSystem(WukongEventBus eventBus, WukongWidgetManager widgetManager) : QuerySystem<MainCharacterComponent, TransformComponent>
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

        Query.ForEachEntity((ref mainCharacter, ref transform, entity) =>
        {
            var mainEntity = new MainCharacterEntity(entity);

            var nickname = mainCharacter.CharacterNickName;
            var position = mainEntity.Pawn?.GetActorLocation() ?? FVector.ZeroVector;
            var ecsPosition = transform.Position.ToFVector();

            widgetManager.UpdatePlayerPosition(nickname, position, ecsPosition);
        });
    }
}