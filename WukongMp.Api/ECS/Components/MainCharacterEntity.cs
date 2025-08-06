using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;

namespace WukongMp.Api.ECS.Components;

public readonly struct MainCharacterEntity(Entity entity) : IEquatable<MainCharacterEntity>
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;
    
    public ref MetadataComponent GetMeta()
        => ref Entity.GetComponent<MetadataComponent>();
    
    public ref MainCharacterComponent GetState()
        => ref Entity.GetComponent<MainCharacterComponent>();
    
    public ref LocalMainCharacterComponent GetLocalState()
        => ref Entity.GetComponent<LocalMainCharacterComponent>();

    public ref readonly TeamComponent GetTeam()
        => ref Entity.GetComponent<TeamComponent>();
    
    public void SetTeam(TeamComponent team)
        => Entity.Set(team);
    
    public bool Equals(MainCharacterEntity other)
        => Entity.Equals(other.Entity);

    public override bool Equals(object? obj)
        => obj is MainCharacterEntity other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
    
    public static bool operator ==(MainCharacterEntity left, MainCharacterEntity right)
        => left.Entity == right.Entity;
    
    public static bool operator !=(MainCharacterEntity left, MainCharacterEntity right)
        => left.Entity != right.Entity;
}