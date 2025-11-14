using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems;

public class DebugViewSystem(WukongEventBus eventBus, WukongWidgetManager widgetManager) : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
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

        Query.ForEachEntity((ref LocalMainCharacterComponent localMainCharacter, ref MainCharacterComponent mainCharacter, Entity entity) =>
        {
            var nickname = mainCharacter.CharacterNickName;
            var position = localMainCharacter.Pawn?.GetActorLocation() ?? FVector.ZeroVector;
            var ecsPosition = mainCharacter.Location.ToFVector();

            widgetManager.UpdatePlayerPosition(nickname, position, ecsPosition);
        });
    }
}
