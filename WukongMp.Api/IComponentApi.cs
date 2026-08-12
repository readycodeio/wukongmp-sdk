using ReadyM.Api.Multiplayer.ECS.Components;

namespace WukongMp.Api;

/// <summary>
/// Provides methods for registering custom networked components.
/// </summary>
public interface IComponentApi
{
    /// <summary>
    /// Register a custom <see cref="INetworkedComponent"/> to be syncronized over the network.
    /// </summary>
    /// <typeparam name="T">Type of the component to register.</typeparam>
    void RegisterComponent<T>() where T : struct, INetworkedComponent;
}