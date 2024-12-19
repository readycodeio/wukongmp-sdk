using UnrealEngine.Engine;

namespace WukongCSharpMod
{
    public class MonsterState
    {
        public byte Id { get; set; }
        public bool Local { get; set; }
        public APawn Pawn { get; set; }
    }
}