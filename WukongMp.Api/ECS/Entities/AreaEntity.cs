using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;

namespace WukongMp.Api.ECS.Entities;

public readonly struct AreaEntity(Entity entity) : IComponent
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;
    
    public RoomComponent Room
        => Entity.GetComponent<RoomComponent>();
    
    public ref MetadataComponent GetMeta()
        => ref Entity.GetComponent<MetadataComponent>();
    
    public ref RoomComponent GetRoom()
        => ref Entity.GetComponent<RoomComponent>();
}