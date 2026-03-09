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

    private static readonly HashSet<BGUCharacterCS> InitializedEqs = [];

    public static void SetActorEquipment(BGUCharacterCS actor, EquipmentState equipment)
    {
        if (actor.GetClass().PathName == Constants.WukongDashengClassPath)
            return;

        var currentEq = GetCurrentEq(actor);
        if (currentEq == null)
        {
            Logging.LogError("Cannot set equipment for actor {ActorName} because current equipment list is unavailable", actor.GetName());
            return;
        }

        foreach (var (position, item) in equipment.GetItems())
        {
            currentEq[position.ToGame()] = item;
        }
    }

    private static BindDictEquipPosition_Int? GetCurrentEq(BGUCharacterCS actor)
    {
        var currentEq = BGU_DataUtil.GetReadOnlyData<IBPC_RoleBaseData, BPC_RoleBaseData>(actor.PlayerState).EquipList;

        if (currentEq == null)
            return null;

        if (InitializedEqs.Add(actor))
        {
            var equipComp = GetEquipComp(actor);
            if (equipComp == null)
            {
                Logging.LogError("Failed to initialize equipment for actor {ActorName}: could not find BUS_EquipComp", actor.GetName());
                return currentEq;
            }

            equipComp.OnAttach();
        }

        return currentEq;
    }

    public static void SetActorEquipment(BGUCharacterCS actor, EquipPosition position, int itemId)
    {
        if (actor.GetClass().PathName == Constants.WukongDashengClassPath)
            return;

        var currentEq = GetCurrentEq(actor);
        if (currentEq == null)
        {
            Logging.LogError("Cannot set equipment for actor {ActorName} because current equipment list is unavailable", actor.GetName());
            return;
        }

        currentEq[position.ToGame()] = itemId;
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