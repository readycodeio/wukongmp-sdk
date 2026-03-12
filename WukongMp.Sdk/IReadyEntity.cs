using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

public interface IReadyEntity<out TSelf>
    where TSelf : struct, IReadyEntity<TSelf>
{
    internal TSelf Construct(WukongClientApi api, Entity type);
    internal void Deconstruct(out WukongClientApi api, out Entity entity);
}