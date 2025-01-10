using b1;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public class MonsterState
    {
        public int Id { get; set; }
        public BUTamerActor Pawn { get; set; }
        public FVector Location { get; set; }
        public FRotator Rotation { get; set; }
        public FVector Velocity { get; set; }
        public FVector MoveAcceleration { get; set; }

        public MonsterState(int id, BUTamerActor pawn)
        {
            Id = id;
            Pawn = pawn;
        }
    }
}