using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Wukong.ECS.Components;

namespace WukongMp.Api.ECS.Entities;

public readonly struct PlayerEntity(Entity entity)
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;

    public ref PlayerComponent GetState()
        => ref Entity.GetComponent<PlayerComponent>();
}
