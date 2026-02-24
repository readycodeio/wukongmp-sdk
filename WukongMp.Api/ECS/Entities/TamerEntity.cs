using System;
using System.Diagnostics.CodeAnalysis;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Entities;

public readonly struct TamerEntity(Entity entity) : IEquatable<TamerEntity>
{
    public static bool IsTamer(Entity entity)
        => !entity.IsNull && entity.HasComponent<TamerComponent>();
    
    public static bool TryGetTamer(Entity entity, [NotNullWhen(true)] out TamerEntity? tamerEntity)
    {
        tamerEntity = null;
        if (!IsTamer(entity))
            return false;

        tamerEntity = new TamerEntity(entity);
        return true;
    }
    
    public readonly Entity Entity = entity;
    
    public static implicit operator Entity(TamerEntity tamerEntity)
        => tamerEntity.Entity;
    
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

    public ref readonly MappingComponent<AActor> GetMappingComponent()
        => ref Entity.GetComponent<MappingComponent<AActor>>();
    
    public BUTamerActor? Tamer
    {
        get
        {
            ref readonly var mappingComp = ref GetMappingComponent();
            // ref var localTamerComp = ref GetLocalTamer();

            // NOTE(api): This test will always work the same
            // if (!localTamerComp.IsTamerSynced)
            //     return null;
            
            var tamer = mappingComp.GameObject as BUTamerActor;
            if (tamer.IsNullOrDestroyed())
                return null;
            
            return tamer;
        }
    }
    
    public void SetTamer(AActor? tamer, bool isSynced)
    {
        Entity.Set(new MappingComponent<AActor>(tamer));
        
        ref var localTamerComp = ref GetLocalTamer();
        if (isSynced)
            localTamerComp.IsTamerSynced = true;
    }

    public BGUCharacterCS? Pawn
    {
        get
        {
            ref var localTamerComp = ref GetLocalTamer();
            
            if (!localTamerComp.IsMonsterActive)
            {
                return null;
            }

            var tamer = Tamer;
            if (tamer == null)
            {
                Logging.LogDebug("Tamer is null or destroyed in getPawn");
                return null;
            }

            var monster = tamer.GetMonster();
            return monster.IsNullOrDestroyed() ? null : monster;
        }
    }
    
    public bool IsTamerValid => !Tamer.IsNullOrDestroyed();

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