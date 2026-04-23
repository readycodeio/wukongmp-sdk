using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;
using AreaScopeComponent = ReadyM.Api.Multiplayer.ECS.Components.AreaScopeComponent;

namespace WukongMp.Api.ECS.Entities;

internal readonly struct AreaEntity(Entity entity) : IComponent
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;
    
    public ref RoomComponent Room
        => ref Entity.GetComponent<RoomComponent>();

    public ref AreaScopeComponent Scope
        => ref Entity.GetComponent<AreaScopeComponent>();

    public ref MetadataComponent GetMeta()
        => ref Entity.GetComponent<MetadataComponent>();

    public ref RoomComponent GetRoom()
        => ref Entity.GetComponent<RoomComponent>();

    public ref MovieComponent GetMovie()
        => ref Entity.GetComponent<MovieComponent>();
}