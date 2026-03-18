using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Api.Implementation;

namespace WukongMp.Sdk.Entities;

public readonly struct ReadyCharacter : IReadyEntity<ReadyCharacter>, 
    IReadyConvertable<ReadyCharacter, ReadyActor>,
    IReadyConvertable<ReadyCharacter, ReadyObject>
{
    internal IWukongClientApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyCharacter(IWukongClientApi api, Entity entity)
    {
        Api = api;
        Entity = entity;
    }
    
    public static implicit operator ReadyObject(ReadyCharacter character)
        => new(character.Api, character.Entity);
    
    public static explicit operator ReadyCharacter(ReadyObject obj)
        => new(obj.Api, obj.Entity);
    
    public static implicit operator ReadyActor(ReadyCharacter tamer)
        => new(tamer.Api, tamer.Entity);
    
    public static explicit operator ReadyCharacter(ReadyActor actor)
        => new(actor.Api, actor.Entity);

    ReadyCharacter IReadyEntity<ReadyCharacter>.Construct(IWukongClientApi api, Entity entity)
        => new(api, entity);

    void IReadyEntity<ReadyCharacter>.Deconstruct(out IWukongClientApi api, out Entity entity)
    {
        api = Api;
        entity = Entity;
    }
}