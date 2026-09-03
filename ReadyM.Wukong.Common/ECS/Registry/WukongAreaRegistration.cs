using ReadyM.Api.ECS.Registry;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

internal class WukongAreaRegistration : IAreaComponentRegistration
{
    public void Register(IAreaComponentRegistry registry)
    {
        registry.RegisterComponent<MovieComponent>();
    }
}
