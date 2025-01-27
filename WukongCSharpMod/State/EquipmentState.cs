using System;
using System.Collections.Generic;
using BtlB1;
using Photon.Client;

namespace WukongCSharpMod.State
{
    public class EquipmentState
    {
        private readonly Dictionary<EquipPosition, int> _equipments = new Dictionary<EquipPosition, int>();

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

            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Head));
            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Upwear));
            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Arm));
            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Foot));
            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Hulu));
            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Weapon));
            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Fabao));
            outStream.WriteByte((byte)state.GetEquipment(EquipPosition.Accessory));

            return 8;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var equipments = new (EquipPosition, int)[]
            {
                (EquipPosition.Head, inStream.ReadByte()),
                (EquipPosition.Upwear, inStream.ReadByte()),
                (EquipPosition.Arm, inStream.ReadByte()),
                (EquipPosition.Foot, inStream.ReadByte()),
                (EquipPosition.Hulu, inStream.ReadByte()),
                (EquipPosition.Weapon, inStream.ReadByte()),
                (EquipPosition.Fabao, inStream.ReadByte()),
                (EquipPosition.Accessory, inStream.ReadByte())
            };

            return new EquipmentState(equipments);
        }
    }
}