using b1;

namespace WukongCSharpMod.State
{
    public class MonsterState : CharacterState
    {
        public string Guid { get; }
        public BUTamerActor Pawn { get; }
        public float? Hp { get; set; }
        public bool IsSynced { get; set; }
        public bool IsTamerValid => Pawn != null && !Pawn.IsDestroyed;

        public MonsterState(string guid, BUTamerActor pawn)
        {
            Guid = guid;
            Pawn = pawn;

            if (pawn.GetMonster() != null)
            {
                TeamId = pawn.GetMonster().GetTeamIDInCS();
            }
            else
            {
                Logging.LogError("Monster is null when creating monster state");
            }
        }

        public MonsterState(string guid, BUTamerActor pawn, int teamId)
        {
            Guid = guid;
            Pawn = pawn;
            TeamId = teamId;
        }
    }
}