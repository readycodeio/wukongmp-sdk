using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Relay.Server.Wukong.ECS.Registry;

namespace WukongMp.Api;

public class WukongNetworkedComponentRegistration : INetworkedComponentRegistration
{
    public void Register(INetworkedComponentRegistry registry)
    {
        WukongCoreApi.RegisterComponents(registry);
    }
}