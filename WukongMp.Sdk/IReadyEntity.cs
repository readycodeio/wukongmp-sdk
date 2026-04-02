using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Api.Implementation;

namespace WukongMp.Sdk;

public interface IReadyEntity<out TSelf>
    where TSelf : struct, IReadyEntity<TSelf>
{
    internal TSelf Construct(IWukongSynchronizationApi api, Entity type);
    internal void Deconstruct(out IWukongSynchronizationApi api, out Entity entity);
}