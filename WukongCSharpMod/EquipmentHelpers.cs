using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using WukongCSharpMod.State;

namespace WukongCSharpMod
{
    public static class EquipmentHelpers
    {
        private static readonly MethodInfo OnChangeEquipReal = typeof(BUS_EquipComp).GetMethod("OnChangeEquipReal", BindingFlags.NonPublic | BindingFlags.Instance);

        public static EquipmentState GetCurrentEquipmentStateForActor(APawn player)
        {
            var roleData = BGU_DataUtil.GetReadOnlyData<IBPC_RoleBaseData, BPC_RoleBaseData>(player.PlayerState);
            return new EquipmentState(roleData.EquipList.Select(kvp => (kvp.Key, kvp.Value)));
        }

        public static void SetRemoteActorEquipment(BGUCharacterCS actor, EquipmentState equipment)
        {
            var equipComp = GetEquipComp(actor);

            foreach (var (position, item) in equipment.GetEquipments())
            {
                OnChangeEquipReal.Invoke(equipComp, new object[] { position, item });
            }
        }

        public static BUS_EquipComp GetEquipComp(BGUCharacterCS actor)
        {
            return Traverse.Create(actor.ActorCompContainerCS).Field<List<UActorCompBaseCS>>("CompCSs").Value
                .FirstOrDefault(x => x is BUS_EquipComp) as BUS_EquipComp;
        }
    }
}