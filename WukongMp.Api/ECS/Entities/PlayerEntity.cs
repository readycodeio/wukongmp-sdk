using Friflo.Engine.ECS;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Api.ECS.Entities;

internal readonly struct PlayerEntity(Entity entity)
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;

    public ref PlayerComponent GetState()
        => ref Entity.GetComponent<PlayerComponent>();
}
