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

            var data = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn);
            Hp = data.GetFloatValue(EBGUAttrFloat.Hp);
        }
    }
}