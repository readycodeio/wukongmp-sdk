using BtlB1;

namespace WukongMp.Api.ECS.Values;

public static class EquipPositionExtensions
{
    public static EquipPosition ToGame(this ReadyM.Relay.Common.Wukong.ECS.Values.EquipPosition value)
        => (EquipPosition)(byte)value;
    
    public static ReadyM.Relay.Common.Wukong.ECS.Values.EquipPosition FromGame(this EquipPosition value)
        => (ReadyM.Relay.Common.Wukong.ECS.Values.EquipPosition)(byte)value;
}