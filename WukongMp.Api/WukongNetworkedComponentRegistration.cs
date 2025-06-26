using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.Wukong;

namespace WukongMp.Api;

public class WukongNetworkedComponentRegistration : INetworkedComponentRegistration
{
    public void Register(INetworkedComponentRegistry registry)
    {
        WukongCoreApi.RegisterComponents(registry);
    }
}