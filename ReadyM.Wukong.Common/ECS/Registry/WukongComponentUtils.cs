using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Tags;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

internal static class WukongComponentUtils
{
    public static void SetupServerMonsterArchetype(EntityBuilderBase b)
        => b.Add<TamerComponent>()
            .Add<AnimationComponent>()
            .Add(new HpComponent
            {
                HpMultiplier = 1.0f,
            })
            .Add<MonsterAnimationComponent>()
            .Add<NicknameComponent>()
            .Add<TeamComponent>()
            .Add<TransformComponent>();

    public static void SetupServerMainCharacterArchetype(EntityBuilderBase b)
        => b.Add(new MainCharacterComponent())
            .Add(new HpComponent
            {
                HpMultiplier = 1.0f,
            })
            .Add<TransformComponent>()
            .Add<TeamComponent>()
            .AddTag<DisallowOwnershipTransferTag>();
}