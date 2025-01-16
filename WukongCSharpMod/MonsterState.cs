using b1;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public class MonsterState
    {
        public string Guid { get; }
        public BUTamerActor Pawn { get; }
        public FVector Location { get; set; }
        public FRotator Rotation { get; set; }
        public FVector Velocity { get; set; }
        public FVector MoveAcceleration { get; set; }
        public float? Hp { get; set; }
        public bool IsSpawned {  get; set; }

        public MonsterState(string guid, BUTamerActor pawn)
        {
            Guid = guid;
            Pawn = pawn;
        }
    }
}