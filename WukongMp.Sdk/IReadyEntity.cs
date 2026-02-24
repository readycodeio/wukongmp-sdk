using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

public interface IReadyEntity<TSelf>
    where TSelf : struct, IReadyEntity<TSelf>
{
    internal TSelf Construct(WukongClientApi api, Entity type);
    internal void Deconstruct(TSelf self, out WukongClientApi api, out Entity entity);
}