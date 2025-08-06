using Friflo.Engine.ECS;

namespace WukongMp.Api.ECS.Components;

public readonly struct PlayerEntity(Entity entity)
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;

    public ref PlayerComponent GetState()
        => ref Entity.GetComponent<PlayerComponent>();
}
