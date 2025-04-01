using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi.State
{
    public abstract class CharacterState
    {
        public FVector Location { get; set; }
        public FRotator Rotation { get; set; }
        public FVector Velocity { get; set; }
        public FVector MoveAcceleration { get; set; }
        public float MaxAcceleration { get; set; }
        public float MaxSpeed { get; set; }
        public EMoveSpeedLevel MoveSpeedLevel { get; set; } = EMoveSpeedLevel.Run;
        public EMoveSpeedLevel MoveSpeedState { get; set; } = EMoveSpeedLevel.Run;
        public int TeamId { get; protected set; }
        public float Hp { get; set; }
        public string NickName { get; set; } = "Unknown";

        public bool IsDead => Hp <= 0;

        private AActor? _markerActor;

        public AActor? MarkerActor
        {
            get
            {
                if (_markerActor != null && _markerActor.IsNullOrDestroyed())
                {
                    Logging.LogWarning("Marker actor is destroyed");
                    return null;
                }

                return _markerActor;
            }
            set => _markerActor = value;
        }

        public abstract void UpdateMarkerPosition();
    }
}