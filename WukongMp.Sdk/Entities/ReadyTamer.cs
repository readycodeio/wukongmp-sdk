using System;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk.Entities;

/// <summary>
/// Represents a tamer (monster) entity in the Wukong multiplayer SDK.
/// </summary>
public readonly struct ReadyTamer
    : IReadyEntity<ReadyTamer>,
        IReadyConvertable<ReadyTamer, ReadyCharacter>,
        IReadyConvertable<ReadyTamer, ReadyActor>,
        IReadyConvertable<ReadyTamer, ReadyObject>
{
    internal IWukongSynchronizationApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyTamer(IWukongSynchronizationApi api, Entity entity)
    {
        Api = api;
        Entity = entity;
    }

    public static implicit operator ReadyObject(ReadyTamer tamer)
        => new(tamer.Api, tamer.Entity);

    public static explicit operator ReadyTamer(ReadyObject obj)
    {
        if (!TamerEntity.IsTamer(obj.Entity))
            throw new InvalidCastException($"The provided {nameof(ReadyObject)} is not {nameof(ReadyTamer)}.");
        return new ReadyTamer(obj.Api, obj.Entity);
    }

    public static implicit operator ReadyCharacter(ReadyTamer tamer)
        => new(tamer.Api, tamer.Entity);

    public static explicit operator ReadyTamer(ReadyCharacter character)
    {
        if (!TamerEntity.IsTamer(character.Entity))
            throw new InvalidCastException($"The provided {nameof(ReadyCharacter)} is not {nameof(ReadyTamer)}.");
        return new(character.Api, character.Entity);
    }

    public static implicit operator ReadyActor(ReadyTamer tamer)
        => new(tamer.Api, tamer.Entity);

    public static explicit operator ReadyTamer(ReadyActor actor)
    {
        if (!TamerEntity.IsTamer(actor.Entity))
            throw new InvalidCastException($"The provided {nameof(ReadyActor)} is not {nameof(ReadyTamer)}.");
        return new(actor.Api, actor.Entity);
    }

    ReadyTamer IReadyEntity<ReadyTamer>.Construct(IWukongSynchronizationApi api, Entity entity)
        => new(api, entity);

    void IReadyEntity<ReadyTamer>.Deconstruct(out IWukongSynchronizationApi api, out Entity entity)
    {
        api = Api;
        entity = Entity;
    }

    public BUTamerActor? Tamer
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            return tamerEntity.Tamer;
        }
    }

    public bool IsMonsterActive
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            return tamerEntity.GetLocalTamer().IsMonsterActive;
        }
    }

    public string? Guid
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            return tamerEntity.GetTamer().Guid;
        }
    }
    
    public int HpScalingPercent
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            return tamerEntity.GetHp().HpMaxMulPercent;
        }
        set
        {
            var tamerEntity = new TamerEntity(Entity);
            ref var hpComp = ref tamerEntity.GetHp();
            hpComp.HpMaxMulPercent = value;
        }
    }

    public bool IsBossOrElite
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            return tamerEntity.GetTamer().IsBossOrElite;
        }
    }
}