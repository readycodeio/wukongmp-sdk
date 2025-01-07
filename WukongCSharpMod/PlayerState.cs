using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public class PlayerState
    {
        public int PhotonId { get; }
        public APawn Pawn { get; set; }
        public bool InJump { get; set; }
        public bool IsFlying { get; set; }
        public bool IsFalling { get; set; }
        public bool IsLandingMove { get; set; }
        public FVector Velocity { get; set; }
        public FVector MoveAcceleration { get; set; }
        public FVector ActorLocation { get; set; }
        public FRotator TurnInplaceTargetRotation { get; set; }
        public bool IsStandRotate { get; set; }
        public float TurnInplaceRemainAngle { get; set; }
        public FRotator ActorRotation { get; set; }

        public PlayerState(int photonId, APawn pawn)
        {
            PhotonId = photonId;
            Pawn = pawn;
        }
    }
}