using b1;
using UnrealEngine.Runtime;

namespace WukongApi.State
{
    public class MonsterState : CharacterState
    {
        public string Guid { get; }
        public BUTamerActor Pawn { get; }
        public float? Hp { get; set; }
        public bool IsSynced { get; set; }
        public bool IsTamerValid => !Pawn.IsNullOrDestroyed();

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