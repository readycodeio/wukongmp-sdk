using System.Collections.Generic;
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
    private static readonly MethodInfo OnChangeEquipReal = typeof(BUS_EquipComp).GetMethod("OnChangeEquipReal", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static EquipmentState GetCurrentEquipmentStateForActor(APawn player)
    {
        var roleData = BGU_DataUtil.GetReadOnlyData<IBPC_RoleBaseData, BPC_RoleBaseData>(player.PlayerState);
        return new EquipmentState(roleData.EquipList.Select(kvp => (kvp.Key.FromGame(), kvp.Value)));
    }

    public static void SetActorEquipment(BGUCharacterCS actor, EquipmentState equipment)
    {
        if (actor.GetClass().PathName == Constants.WukongDashengClassPath)
            return;

        var currentEq = BGU_DataUtil.GetReadOnlyData<IBPC_RoleBaseData, BPC_RoleBaseData>(actor.PlayerState).EquipList;
        BUS_EquipComp? equipComp = null; // lazily initialized

        foreach (var (position, item) in equipment.GetItems())
        {
            if (currentEq[position.ToGame()] != item)
            {
                equipComp ??= GetEquipComp(actor);
                OnChangeEquipReal.Invoke(equipComp, [position.ToGame(), item]);
            }
        }
    }

    public static void SetActorEquipment(BGUCharacterCS actor, EquipPosition position, int itemId)
    {
        if (actor.GetClass().PathName == Constants.WukongDashengClassPath)
            return;

        var equipComp = GetEquipComp(actor);
        OnChangeEquipReal.Invoke(equipComp, [position.ToGame(), itemId]);
    }

    // Expensive, consider compiling
    private static BUS_EquipComp? GetEquipComp(BGUCharacterCS actor)
    {
        return Traverse
            .Create(actor.ActorCompContainerCS)
            .Field<List<UActorCompBaseCS>>("CompCSs").Value
            .FirstOrDefault(x => x is BUS_EquipComp) as BUS_EquipComp;
    }
}