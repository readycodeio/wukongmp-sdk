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
        registry.RegisterComponent<HpComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<NicknameComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<TeamComponent>(DeliveryMethod.ReliableOrdered);
        
        // Tamer (area-scoped)
        registry.RegisterComponent<TamerComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<AnimationComponent>();
        registry.RegisterComponent<MonsterAnimationComponent>();

        // Main character (area-scoped)
        registry.RegisterComponent<MainCharacterComponent>();
        registry.RegisterComponent<PvPComponent>(DeliveryMethod.ReliableOrdered); // TODO: Move to PvP mod (server-side)

        // Area (global, scope)
        registry.RegisterComponent<RoomComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<MovieComponent>(DeliveryMethod.ReliableOrdered);

        // Player (global, scope)
        registry.RegisterComponent<PlayerComponent>(DeliveryMethod.ReliableOrdered);
        
        // PvP State (global)
        registry.RegisterComponent<PvpStateComponent>(DeliveryMethod.ReliableOrdered); // TODO: Move to PvP mod (server-side)
    }
}