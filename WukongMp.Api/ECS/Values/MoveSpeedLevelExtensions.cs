using b1;
using ReadyM.Wukong.Common.ECS.Values;

namespace WukongMp.Api.ECS.Values;

public static class MoveSpeedLevelExtensions
{
    public static EMoveSpeedLevel ToGame(this MoveSpeedLevel value)
        => (EMoveSpeedLevel)(byte)value;

    public static MoveSpeedLevel FromGame(this EMoveSpeedLevel value)
        => (MoveSpeedLevel)(byte)value;
}