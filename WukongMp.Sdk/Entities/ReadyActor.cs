using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Api.Implementation;

namespace WukongMp.Sdk.Entities;

/// A ReadyActor is an entity that has a mapped Pawn.
public readonly struct ReadyActor : IReadyEntity<ReadyActor>, IReadyConvertable<ReadyActor, ReadyObject>
{
    internal IWukongClientApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyActor(IWukongClientApi api, Entity entity)
    {
        Api = api;
        Entity = entity;
    }

    public static implicit operator ReadyObject(ReadyActor actor)
        => new(actor.Api, actor.Entity);

    public static explicit operator ReadyActor(ReadyObject obj)
        => new(obj.Api, obj.Entity);

    ReadyActor IReadyEntity<ReadyActor>.Construct(IWukongClientApi api, Entity entity)
        => new(api, entity);

    void IReadyEntity<ReadyActor>.Deconstruct(out IWukongClientApi api, out Entity entity)
    {
        api = Api;
        entity = Entity;
    }
}