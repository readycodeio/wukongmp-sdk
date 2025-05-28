using System;
using System.Collections.Generic;
using BtlB1;
using LiteNetLib.Utils;

namespace WukongMp.Api.Old.State
{
    public class EquipmentState
    {
        private readonly int[] equipments = [0, 0, 0, 0, 0, 0, 0, 0];

        private EquipmentState(int[] eq)
        {
            equipments = eq;
        }

        public EquipmentState(IEnumerable<(EquipPosition, int)> equipments)
        {
            foreach (var (position, id) in equipments)
            {
                this.equipments[(int)position] = id;
            }
        }

        public void SetEquipment(EquipPosition position, int eqId)
        {
            equipments[(int)position] = eqId;
        }

        public IEnumerable<(EquipPosition, int)> GetEquipments()
        {
            for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
            {
                var id = equipments[i];
                if (id != 0)
                {
                    yield return ((EquipPosition)i, id);
                }
            }
        }

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            outStream.PutArray(((EquipmentState)customObject).equipments);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var eq = inStream.GetIntArray();
            if (eq.Length != (int)EquipPosition.EnumMax)
            {
                throw new ArgumentException($"Invalid equipment state length: {eq.Length}");
            }

            return new EquipmentState(eq);
        }
    }
}