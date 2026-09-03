using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk.Entities;

public readonly struct ReadyObject : IReadyEntity<ReadyObject>, IReadyConvertable<ReadyObject, ReadyObject>
{
    internal IWukongSynchronizationApi Api { get; }
    internal Entity Entity { get; }

    // TODO: Internal
    public ReadyObject(IWukongSynchronizationApi api, Entity entity)
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

    public ref T Get<T>() where T : struct, IComponent
    {
        return ref Entity.GetComponent<T>();
    }
}