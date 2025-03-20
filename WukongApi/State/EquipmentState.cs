using System;
using System.Collections.Generic;
using BtlB1;
using Photon.Client;

namespace WukongApi.State
{
    public class EquipmentState
    {
        private readonly Dictionary<EquipPosition, int> _equipments = new();

        private EquipmentState() { }

        public EquipmentState(IEnumerable<(EquipPosition, int)> equipments)
        {
            foreach (var (position, id) in equipments)
            {
                _equipments[position] = id;
            }
        }

        public void SetEquipment(EquipPosition position, int eqId)
        {
            _equipments[position] = eqId;
        }

        public int GetEquipment(EquipPosition position)
        {
            return _equipments.GetValueOrDefault(position, 0);
        }

        public IEnumerable<(EquipPosition, int)> GetEquipments()
        {
            foreach (var (position, id) in _equipments)
            {
                yield return (position, id);
            }
        }

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var state = (EquipmentState)customObject;

            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Head)), 0, 4);
            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Upwear)), 0, 4);
            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Arm)), 0, 4);
            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Foot)), 0, 4);
            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Hulu)), 0, 4);
            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Weapon)), 0, 4);
            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Fabao)), 0, 4);
            outStream.Write(BitConverter.GetBytes(state.GetEquipment(EquipPosition.Accessory)), 0, 4);

            return 8 * 4;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var intBuffer = new byte[4];
            var eq = new EquipmentState();

            for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
            {
                inStream.Read(intBuffer, 0, 4);
                var part = BitConverter.ToInt32(intBuffer, 0);
                eq.SetEquipment((EquipPosition)i, part);
            }

            return eq;
        }
    }
}