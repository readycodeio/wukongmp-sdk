using LiteNetLib;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

public class WukongNetworkedComponentRegistration : INetworkedComponentRegistration
{
    public void Register(INetworkedComponentRegistry registry)
    {
        // Tamer
        registry.RegisterComponent<TamerComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<AnimationComponent>();
        registry.RegisterComponent<HpComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<MonsterAnimationComponent>();
        registry.RegisterComponent<NicknameComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<TeamComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<TransformComponent>();
        // FIXME: EquipmentComponent

        // Main character
        registry.RegisterComponent<MainCharacterComponent>();

        // Room
        registry.RegisterComponent<RoomComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<MovieComponent>(DeliveryMethod.ReliableOrdered);

        // Player
        registry.RegisterComponent<PlayerComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<PvPComponent>(DeliveryMethod.ReliableOrdered);

        // PvP state
        registry.RegisterComponent<PvpStateComponent>(DeliveryMethod.ReliableOrdered);
    }
}