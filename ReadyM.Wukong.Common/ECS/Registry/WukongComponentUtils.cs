using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Tags;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

public static class WukongComponentUtils
{
    public static void SetupServerMonsterArchetype(EntityBuilder b)
        => b.Add(new TamerComponent
            {
                HoldingPlayers = []
            })
            .Add<AnimationComponent>()
            .Add(new HpComponent
            {
                HpMultiplier = 1.0f,
            })
            .Add<MonsterAnimationComponent>()
            .Add<NicknameComponent>()
            .Add<TeamComponent>()
            .Add<TransformComponent>();

    public static void SetupServerMainCharacterArchetype(EntityBuilder b)
        => b.Add(new MainCharacterComponent())
            .Add(new HpComponent
            {
                HpMultiplier = 1.0f,
            })
            .Add<TransformComponent>()
            .Add<TeamComponent>()
            .Add<PvPComponent>()
            .AddTag<DisallowOwnershipTransferTag>();

    public static void SetupServerPvpStateArchetype(EntityBuilder b)
        => b.Add<PvpStateComponent>();
}