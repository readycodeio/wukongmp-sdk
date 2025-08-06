using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;

namespace WukongMp.Api.ECS.Components;

public readonly struct AreaEntity(Entity entity) : IComponent
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;
    
    public WukongRoomComponent Room
        => Entity.GetComponent<WukongRoomComponent>();
    
    public ref MetadataComponent GetMeta()
        => ref Entity.GetComponent<MetadataComponent>();
    
    public ref WukongRoomComponent GetRoom()
        => ref Entity.GetComponent<WukongRoomComponent>();
}