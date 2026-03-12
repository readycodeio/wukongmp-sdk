using System;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using WukongMp.Api.ECS.Entities;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk.Entities;

public readonly struct ReadyTamer
    : IReadyEntity<ReadyTamer>,
        IReadyConvertable<ReadyTamer, ReadyCharacter>,
        IReadyConvertable<ReadyTamer, ReadyActor>,
        IReadyConvertable<ReadyTamer, ReadyObject>
{
    internal WukongClientApi Api { get; }
    internal Entity Entity { get; }

    internal ReadyTamer(WukongClientApi api, Entity entity)
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

    ReadyTamer IReadyEntity<ReadyTamer>.Construct(WukongClientApi api, Entity entity)
        => new(api, entity);

    void IReadyEntity<ReadyTamer>.Deconstruct(out WukongClientApi api, out Entity entity)
    {
        api = Api;
        entity = Entity;
    }

    public bool IsMonsterActive
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            return tamerEntity.GetLocalTamer().IsMonsterActive;
        }
    }

    public float HpMultiplier
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            ref var hpComp = ref tamerEntity.GetHp();
            return hpComp.HpMultiplier;
        }
        set
        {
            var tamerEntity = new TamerEntity(Entity);
            ref var hpComp = ref tamerEntity.GetHp();
            hpComp.HpMultiplier = value;
        }
    }

    public string? Guid
    {
        get
        {
            var tamerEntity = new TamerEntity(Entity);
            ref var tamerComp = ref tamerEntity.GetTamer();
            return tamerComp.Guid;
        }
    }

    public bool IsBossOrElite
    {
        get
        {
            if (this.Pawn == null)
                return false;

            var info = BGW_GameDB.GetUnitBattleInfoExtendDesc(this.Pawn.GetFinalBattleInfoExtendID());
            var healthBarType = info?.BloodBarType;
            return healthBarType is EBGUBloodBarType.BossBar or EBGUBloodBarType.EliteBar;
        }
    }
}