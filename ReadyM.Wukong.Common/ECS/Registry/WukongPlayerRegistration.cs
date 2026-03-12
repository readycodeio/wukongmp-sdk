using ReadyM.Api.ECS.Registry;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

internal class WukongPlayerRegistration : IPlayerComponentRegistration
{
    public void Register(IPlayerComponentRegistry registry)
    {
        registry.RegisterComponent<PlayerComponent>();
    }
}