using ReadyM.Relay.Common.Wukong.ECS.Values;

namespace WukongMp.Api.ECS.Values;

public static class EquipPositionExtensions
{
    public static BtlB1.EquipPosition ToGame(this EquipPosition value)
        => (BtlB1.EquipPosition)(byte)value;
    
    public static EquipPosition FromGame(this BtlB1.EquipPosition value)
        => (EquipPosition)(byte)value;
}