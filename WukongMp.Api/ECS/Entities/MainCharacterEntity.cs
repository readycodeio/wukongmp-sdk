using System;
using System.Diagnostics.CodeAnalysis;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Entities;

internal readonly struct MainCharacterEntity(Entity entity) : IEquatable<MainCharacterEntity>
{
    public static bool IsMainCharacter(Entity entity)
        => !entity.IsNull && entity.HasComponent<MainCharacterComponent>();

    public static bool TryGetMainCharacter(Entity entity, [NotNullWhen(true)] out MainCharacterEntity? mainEntity)
    {
        mainEntity = null;
        if (!IsMainCharacter(entity))
            return false;

        mainEntity = new MainCharacterEntity(entity);
        return true;
    }

    public readonly Entity Entity = entity;

    public static implicit operator Entity(MainCharacterEntity mainCharacterEntity)
        => mainCharacterEntity.Entity;

    public bool IsNull
        => Entity.IsNull;

    public ref MetadataComponent GetMeta()
        => ref Entity.GetComponent<MetadataComponent>();

    public ref MainCharacterComponent GetState()
        => ref Entity.GetComponent<MainCharacterComponent>();

    public ref HpComponent GetHp()
        => ref Entity.GetComponent<HpComponent>();

    public ref readonly TransformComponent GetTransform()
        => ref Entity.GetComponent<TransformComponent>();

    public ref LocalMainCharacterComponent GetLocalState()
        => ref Entity.GetComponent<LocalMainCharacterComponent>();

    public ref TeamComponent GetTeam()
        => ref Entity.GetComponent<TeamComponent>();

    public ref MarkerComponent GetMarker()
        => ref Entity.GetComponent<MarkerComponent>();

    public ref PvPComponent GetPvP()
        => ref Entity.GetComponent<PvPComponent>();

    public void SetTeam(TeamComponent team)
        => Entity.Set(team);

    public ref readonly MappingComponent<AActor> GetMappingComponent()
        => ref Entity.GetComponent<MappingComponent<AActor>>();

    public BGUCharacterCS? UnsyncedPawn
    {
        get
        {
            ref readonly var mappingComp = ref GetMappingComponent();

            var pawn = mappingComp.GameObject as BGUCharacterCS;

            if (pawn.IsNullOrDestroyed())
            {
                Logging.LogWarning("Player pawn is null or destroyed");
                return null;
            }

            return pawn;
        }
    }

    public BGUCharacterCS? Pawn
    {
        get
        {
            ref readonly var mappingComp = ref GetMappingComponent();
            ref readonly var localMainComp = ref GetLocalState();

            if (!localMainComp.IsPlayerSynced)
            {
                return null;
            }

            var pawn = mappingComp.GameObject as BGUCharacterCS;

            if (pawn.IsNullOrDestroyed())
            {
                Logging.LogWarning("Player pawn is null or destroyed");
                return null;
            }

            return pawn;
        }
    }

    public bool HasPawn
        => Pawn != null;

    public bool HasUnsyncedPawn
        => UnsyncedPawn != null;

    public void SetPawn(BGUCharacterCS pawn, bool isSynced)
    {
        if (pawn.IsNullOrDestroyed())
            throw new ArgumentNullException(nameof(pawn));

        ref readonly var mappingComp = ref GetMappingComponent();
        var lastPawn = mappingComp.GameObject as BGUCharacterCS;

        Entity.Set(new MappingComponent<AActor>(pawn));

        // NOTE(api): This line has to come after component manipulation as this causes structural changes that invalidate the ref
        ref var localMainComp = ref GetLocalState();

        if (isSynced)
            localMainComp.IsPlayerSynced = true;

        localMainComp.LastPawn = lastPawn.IsNullOrDestroyed() ? null : lastPawn;
    }

    public bool Equals(MainCharacterEntity other)
        => Entity.Equals(other.Entity);

    public override bool Equals(object? obj)
        => obj is MainCharacterEntity other && Equals(other);

    public override int GetHashCode()
        => Entity.GetHashCode();

    public override string ToString()
        => $"MainCharacterEntity({Entity.GetNetId()})";

    public static bool operator ==(MainCharacterEntity left, MainCharacterEntity right)
        => left.Entity == right.Entity;

    public static bool operator !=(MainCharacterEntity left, MainCharacterEntity right)
        => left.Entity != right.Entity;

    public NetworkId GetNetId()
        => Entity.GetNetId();
}