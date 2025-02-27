using BtlShare;
using Photon.Client;
using System;

namespace WukongApi
{
    public class InputActionData
    {
        public EInputActionType InputActionType { get; }
        public bool IsRelease { get; }
        public int SkillID { get; }
        public int DescID { get; }
        public int ItemID { get; }

        public InputActionData(EInputActionType inputActionType, bool isRelease, int skillID, int descID, int itemID)
        {
            InputActionType= inputActionType;
            IsRelease = isRelease;
            SkillID = skillID;
            DescID = descID;
            ItemID = itemID;
        }

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (InputActionData)customObject;
            outStream.WriteByte((byte)data.InputActionType);
            outStream.Write(BitConverter.GetBytes(data.IsRelease), 0, 1);
            outStream.Write(BitConverter.GetBytes(data.SkillID), 0, 4);
            outStream.Write(BitConverter.GetBytes(data.DescID), 0, 4);
            outStream.Write(BitConverter.GetBytes(data.ItemID), 0, 4);

            return (short)(1 + 1 + 12);
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var inputActionType = (EInputActionType)inStream.ReadByte();

            var isReleaseBytes = new byte[1];
            inStream.Read(isReleaseBytes, 0, 1);
            var isRelease = BitConverter.ToBoolean(isReleaseBytes, 0);

            var intBytes = new byte[4];

            inStream.Read(intBytes, 0, 4);
            var skillID = BitConverter.ToInt32(intBytes, 0);

            inStream.Read(intBytes, 0, 4);
            var descID = BitConverter.ToInt32(intBytes, 0);

            inStream.Read(intBytes, 0, 4);
            var itemID = BitConverter.ToInt32(intBytes, 0);

            return new InputActionData(inputActionType, isRelease, skillID, descID, itemID);
        }

    }
}
