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
                HpMaxMulPercent = 100,
            })
            .Add<MonsterAnimationComponent>()
            .Add<NicknameComponent>()
            .Add<TeamComponent>()
            .Add<TransformComponent>()
            .AddTag<AllowOwnershipTransferOnScopeLeaveTag>();

    public static void SetupServerMainCharacterArchetype(EntityBuilderBase b)
        => b.Add(new MainCharacterComponent())
            .Add<NicknameComponent>()
            .Add(new HpComponent
            {
                HpMaxMulPercent = 100,
            })
            .Add<TransformComponent>()
            .Add<TeamComponent>()
            .Add<PvPComponent>();

    public static void SetupServerPvpStateArchetype(EntityBuilderBase b)
        => b.Add(new PvpStateComponent());
}
