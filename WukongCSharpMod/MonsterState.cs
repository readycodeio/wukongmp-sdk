using b1;
using BtlShare;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public class MonsterState
    {
        public int Id { get; }
        public BUTamerActor Pawn { get; }
        public FVector Location { get; set; }
        public FRotator Rotation { get; set; }
        public FVector Velocity { get; set; }
        public FVector MoveAcceleration { get; set; }
        public float Hp { get; set; }

        public MonsterState(int id, BUTamerActor pawn)
        {
            Id = id;
            Pawn = pawn;
            Hp = 1; // TODO: Hack, get the HP from the monster
        }
    }
}