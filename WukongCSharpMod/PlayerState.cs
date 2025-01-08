using b1;
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
        public bool IsAttacking { get; set; }
        public bool OrientRotationToMovement { get; set; }
        public EMoveSpeedLevel MoveSpeedLevel { get; set; } = EMoveSpeedLevel.Run;
        public EMoveSpeedLevel MoveSpeedState { get; set; } = EMoveSpeedLevel.Run;

        public PlayerState(int photonId, APawn pawn)
        {
            PhotonId = photonId;
            Pawn = pawn;
        }

        public override string ToString()
        {
            return $"PlayerState(PhotonId: {PhotonId}, InJump: {InJump}, IsFlying: {IsFlying}, IsFalling: {IsFalling}, IsLandingMove: {IsLandingMove}, Velocity: {Velocity}, MoveAcceleration: {MoveAcceleration}, ActorLocation: {ActorLocation}, TurnInplaceTargetRotation: {TurnInplaceTargetRotation}, IsStandRotate: {IsStandRotate}, TurnInplaceRemainAngle: {TurnInplaceRemainAngle}, ActorRotation: {ActorRotation}, IsAttacking: {IsAttacking}, OrientRotationToMovement: {OrientRotationToMovement}, MoveSpeedLevel: {MoveSpeedLevel}, MoveSpeedState: {MoveSpeedState})";
        }
    }
}