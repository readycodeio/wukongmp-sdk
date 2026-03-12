using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using b1;
using HarmonyLib;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.WukongUtils;

public static class EquipmentUtils
{
    private delegate void OnChangeEquipRealDelegate(BUS_EquipComp equipComp, BtlB1.EquipPosition position, int item);

    private static readonly OnChangeEquipRealDelegate OnChangeEquipReal =
        (OnChangeEquipRealDelegate)Delegate.CreateDelegate(
            typeof(OnChangeEquipRealDelegate),
            null,
            typeof(BUS_EquipComp).GetMethod("OnChangeEquipReal", BindingFlags.NonPublic | BindingFlags.Instance)!);

    // Weak cache: entries disappear when actor is collected.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BGUCharacterCS, BUS_EquipComp> EquipCompCache = new();

    public static EquipmentState GetCurrentEquipmentStateForActor(APawn player)
    {
        var roleData = BGU_DataUtil.GetReadOnlyData<IBPC_RoleBaseData, BPC_RoleBaseData>(player.PlayerState);
        return new EquipmentState(roleData.EquipList.Select(kvp => (kvp.Key.FromGame(), kvp.Value)));
    }

    public static void SetActorEquipment(BGUCharacterCS actor, EquipmentState equipment)
    {
        if (ShouldSkip(actor))
            return;

        var equipComp = GetOrCacheEquipComp(actor);
        if (equipComp is null)
            return;

        var actual = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_EquipData>(actor);
        foreach (var (position, item) in equipment.GetItems())
        {
            if (!actual.MapEquip.TryGetValue(position.ToGame(), out var current) || current != item)
            {
                OnChangeEquipReal(equipComp, position.ToGame(), item);
            }
        }
    }

    public static void SetActorEquipment(BGUCharacterCS actor, EquipPosition position, int item)
    {
        if (ShouldSkip(actor))
            return;

        var equipComp = GetOrCacheEquipComp(actor);
        if (equipComp is null)
            return;

        var actual = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_EquipData>(actor);
        if (!actual.MapEquip.TryGetValue(position.ToGame(), out var current) || current != item)
        {
            OnChangeEquipReal(equipComp, position.ToGame(), item);
        }
    }

    private static bool ShouldSkip(BGUCharacterCS actor)
    {
        return actor.GetClass().PathName == Constants.WukongDashengClassPath;
    }

    private static BUS_EquipComp? GetOrCacheEquipComp(BGUCharacterCS actor)
    {
        if (EquipCompCache.TryGetValue(actor, out var cached))
        {
            Debug.Assert(cached.GetOwner() == actor, "cached.GetOwner() == actor");
            return cached;
        }

        var resolved = ResolveEquipComp(actor);
        if (resolved is not null)
            EquipCompCache.Add(actor, resolved);

        return resolved;
    }

    // Slow path, used only on cache miss.
    private static BUS_EquipComp? ResolveEquipComp(BGUCharacterCS actor)
    {
        return Traverse
            .Create(actor.ActorCompContainerCS)
            .Field<List<UActorCompBaseCS>>("CompCSs").Value?
            .FirstOrDefault(static x => x is BUS_EquipComp) as BUS_EquipComp;
    }

    // Optional: call this if you know an actor rebuilt its components.
    public static void InvalidateEquipCompCache(BGUCharacterCS actor)
    {
        EquipCompCache.Remove(actor);
    }
}