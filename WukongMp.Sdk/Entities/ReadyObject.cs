using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Api.Implementation;

namespace WukongMp.Sdk.Entities;

public readonly struct ReadyObject : IReadyEntity<ReadyObject>, IReadyConvertable<ReadyObject, ReadyObject>
{
    internal IWukongSynchronizationApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyObject(IWukongSynchronizationApi api, Entity entity)
    {
        Api = api;
        Entity = entity;
    }

    ReadyObject IReadyEntity<ReadyObject>.Construct(IWukongSynchronizationApi api, Entity type) => new(api, type);

    void IReadyEntity<ReadyObject>.Deconstruct(out IWukongSynchronizationApi api, out Entity entity)
    {
        api = Api;
        entity = Entity;
    }
}