using b1;
using UnrealEngine.Runtime;

namespace WukongCSharpMod.State
{
    public abstract class CharacterState
    {
        public FVector Location { get; set; }
        public FRotator Rotation { get; set; }
        public FVector Velocity { get; set; }
        public FVector MoveAcceleration { get; set; }
        public EMoveSpeedLevel MoveSpeedLevel { get; set; } = EMoveSpeedLevel.Run;
        public EMoveSpeedLevel MoveSpeedState { get; set; } = EMoveSpeedLevel.Run;
        public int TeamID { get; set; }
    }
}