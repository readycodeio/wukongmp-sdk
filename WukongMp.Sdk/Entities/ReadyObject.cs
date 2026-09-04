using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

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

    /// <summary>
    /// Whether the entity behind this handle no longer exists.
    /// </summary>
    public bool IsNull => Entity.IsNull;

    /// <summary>
    /// Reads a component, throwing if the entity is gone.
    /// </summary>
    public ref T Get<T>() where T : struct, IComponent
    {
        return ref Entity.GetComponent<T>();
    }

    /// <summary>
    /// Reads a copy of a component, or returns false when the entity is gone or does not carry it.
    /// </summary>
    public bool TryGet<T>(out T value) where T : struct, IComponent
    {
        if (Entity.IsNull || !Entity.HasComponent<T>())
        {
            value = default;
            return false;
        }

        value = Entity.GetComponent<T>();
        return true;
    }
}