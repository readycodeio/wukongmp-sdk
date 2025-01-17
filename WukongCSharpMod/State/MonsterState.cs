using b1;

namespace WukongCSharpMod.State
{
    public class MonsterState : CharacterState
    {
        public string Guid { get; }
        public BUTamerActor Pawn { get; }
        public float? Hp { get; set; }
        public bool IsSynced {  get; set; }

        public MonsterState(string guid, BUTamerActor pawn)
        {
            Guid = guid;
            Pawn = pawn;
        }
    }
}