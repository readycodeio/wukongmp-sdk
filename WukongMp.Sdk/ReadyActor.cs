using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

public readonly struct ReadyActor : IReadyEntity<ReadyActor>, IReadyConvertable<ReadyActor, ReadyObject>
{
    internal WukongClientApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyActor(WukongClientApi api, Entity entity)
    {
        Api = api;
        Entity = entity;
    }
    
    public static implicit operator ReadyObject(ReadyActor actor)
        => new(actor.Api, actor.Entity);
    
    public static explicit operator ReadyActor(ReadyObject obj)
        => new(obj.Api, obj.Entity);

    ReadyActor IReadyEntity<ReadyActor>.Construct(WukongClientApi api, Entity entity)
        => new(api, entity);

    void IReadyEntity<ReadyActor>.Deconstruct(ReadyActor self, out WukongClientApi api, out Entity entity)
    {
        api = self.Api;
        entity = self.Entity;
    }
}