using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

public readonly struct ReadyObject : IReadyEntity<ReadyObject>, IReadyConvertable<ReadyObject, ReadyObject>
{
    internal WukongClientApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyObject(WukongClientApi api, Entity entity)
    {
        Api = api;
        Entity = entity;
    }

    ReadyObject IReadyEntity<ReadyObject>.Construct(WukongClientApi api, Entity type)
        => new ReadyObject(api, type);

    void IReadyEntity<ReadyObject>.Deconstruct(ReadyObject self, out WukongClientApi api, out Entity entity)
    {
        api = self.Api;
        entity = self.Entity;
    }
}