using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Entities;

public readonly struct TamerEntity(Entity entity) : IEquatable<TamerEntity>
{
    public readonly Entity Entity = entity;
    
    public bool IsNull
        => Entity.IsNull;

    public ref MetadataComponent GetMeta()
        => ref Entity.GetComponent<MetadataComponent>();
    
    public ref readonly TeamComponent GetTeam()
        => ref Entity.GetComponent<TeamComponent>();

    public void SetTeam(TeamComponent team)
        => Entity.Set(team);

    public ref NicknameComponent GetNickname()
        => ref Entity.GetComponent<NicknameComponent>();
    
    public bool HasMarker()
        => Entity.HasComponent<MarkerComponent>();
    
    public void AddMarker()
        => Entity.AddComponent<MarkerComponent>();
    
    public ref MarkerComponent GetMarker()
        => ref Entity.GetComponent<MarkerComponent>();
    
    public ref TransformComponent GetTransform()
        => ref Entity.GetComponent<TransformComponent>();
    
    public ref PhysicalMoveComponent GetPhysicalMove()
        => ref Entity.GetComponent<PhysicalMoveComponent>();

    public ref TamerComponent GetTamer()
        => ref Entity.GetComponent<TamerComponent>();

    public ref LocalTamerComponent GetLocalTamer()
        => ref Entity.GetComponent<LocalTamerComponent>();
    
    public ref HpComponent GetHp()
        => ref Entity.GetComponent<HpComponent>();

    public ref AnimationComponent GetAnimation()
        => ref Entity.GetComponent<AnimationComponent>();
    
    public ref MonsterAnimationComponent GetMonsterAnimation()
        => ref Entity.GetComponent<MonsterAnimationComponent>();

    public bool Equals(TamerEntity other)
        => Entity.Equals(other.Entity);

    public override bool Equals(object? obj)
        => obj is TamerEntity other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();
    
    public static bool operator ==(TamerEntity left, TamerEntity right)
        => left.Entity == right.Entity;

    public static bool operator !=(TamerEntity left, TamerEntity right)
        => left.Entity != right.Entity;
}