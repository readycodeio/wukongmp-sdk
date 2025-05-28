using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongMp.Api.State
{
    public abstract class CharacterState
    {
        public abstract BGUCharacterCS? Pawn { get; set; }
        public short PeerId { get; protected set; }

        public FVector Location { get; set; }
        public FRotator Rotation { get; set; }
        public FVector Velocity { get; set; }
        public FVector MoveAcceleration { get; set; }
        public EMoveSpeedLevel MoveSpeedLevel { get; set; } = EMoveSpeedLevel.Run;
        public EMoveSpeedLevel MoveSpeedState { get; set; } = EMoveSpeedLevel.Run;
        public int TeamId { get; protected set; }
        public float Hp { get; set; }
        public string NickName { get; set; } = "";

        public bool RunImmobilizePatches { get; set; }

        public MontageState MontageState { get; set; }

        public bool IsDead => Hp <= 0;

        public AActor? MarkerActor
        {
            get
            {
                if (field != null && field.IsNullOrDestroyed())
                {
                    Logging.LogTrace("Marker actor is destroyed");
                    return null;
                }

                return field;
            }
            set;
        }

        public void UpdateMarkerPosition()
        {
            if (MarkerActor != null)
            {
                if (Pawn == null)
                {
                    Logging.LogError("Pawn is null");
                    return;
                }

                var markerHeight = Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1;
                MarkerActor.SetActorLocation(Pawn.GetActorLocation() + new FVector(0, 0, markerHeight), false, out _, true);
            }
        }
    }
}