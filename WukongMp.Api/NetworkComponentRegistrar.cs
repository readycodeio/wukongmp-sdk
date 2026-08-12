using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace WukongMp.Api;

internal class NetworkComponentRegistrar(IDependencyContainer container) : IComponentApi
{
    private class Registration<T> : INetworkedComponentRegistration where T : struct, INetworkedComponent
    {
        public void Register(INetworkedComponentRegistry registry)
        {
            registry.RegisterComponent<T>();
        }
    }

    public void RegisterComponent<T>() where T : struct, INetworkedComponent
    {
        container.RegisterSingleton<INetworkedComponentRegistration>(new Registration<T>());
    }
}