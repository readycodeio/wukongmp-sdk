using System;
using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

[Obsolete("Part of old 0.x SDK. Use SDK 1.0 methods instead.")]
public interface IReadyEntity<out TSelf>
    where TSelf : struct, IReadyEntity<TSelf>
{
    internal TSelf Construct(IWukongSynchronizationApi api, Entity type);
    internal void Deconstruct(out IWukongSynchronizationApi api, out Entity entity);
}