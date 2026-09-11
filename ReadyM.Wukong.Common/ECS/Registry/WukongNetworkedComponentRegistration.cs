using LiteNetLib;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

internal class WukongNetworkedComponentRegistration : INetworkedComponentRegistration
{
    public void Register(INetworkedComponentRegistry registry)
    {
        // Shared
        registry.RegisterComponent<TransformComponent>();
        registry.RegisterComponent<VelocityComponent>(DeliveryMethod.ReliableOrdered); // unreliable might cause non-zero acceleration and running in place
        registry.RegisterComponent<HpComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<NicknameComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<TeamComponent>(DeliveryMethod.ReliableOrdered);

        // Tamer (area-scoped)
        registry.RegisterComponent<TamerComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<MonsterAnimationComponent>(DeliveryMethod.ReliableOrdered);

        // Main character (area-scoped)
        registry.RegisterComponent<MainCharacterComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<PlayerAnimationComponent>(DeliveryMethod.ReliableOrdered);

        // Area (global, scope)
        registry.RegisterComponent<MovieComponent>(DeliveryMethod.ReliableOrdered);

        // Player (global, scope)
        registry.RegisterComponent<PlayerComponent>(DeliveryMethod.ReliableOrdered);
    }
}