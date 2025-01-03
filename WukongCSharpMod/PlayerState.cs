using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public class PlayerState
    {
        public int PhotonId { get; }
        public APawn Pawn { get; set; }
        public bool IsFlying { get; set; }
        public bool IsFalling { get; set; }
        public bool IsLastFrameFalling { get; set; }
        public bool IsLandingMove { get; set; }
        public FVector ActorLocation { get; set; }
        public FRotator ActorRotation { get; set; }
        public FVector ForwardVector { get; set; }
        public FVector Velocity { get; set; }
        public FVector LeftFootPos { get; set; }

        public FVector RightFootPos { get; set; }
        public float VerticalSpeed { get; set; }
        public FVector MoveAcceleration { get; set; }

        public PlayerState(int photonId, APawn pawn)
        {
            PhotonId = photonId;
            Pawn = pawn;
        }
    }
}