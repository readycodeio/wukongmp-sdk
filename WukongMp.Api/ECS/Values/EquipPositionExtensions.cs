using BtlB1;

namespace WukongMp.Api.ECS.Values;

public static class EquipPositionExtensions
{
    public static EquipPosition ToGame(this ReadyM.Wukong.Common.ECS.Values.EquipPosition value)
        => (EquipPosition)(byte)value;
    
    public static ReadyM.Wukong.Common.ECS.Values.EquipPosition FromGame(this EquipPosition value)
        => (ReadyM.Wukong.Common.ECS.Values.EquipPosition)(byte)value;
}