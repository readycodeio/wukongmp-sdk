using b1;
using UnrealEngine.Runtime;

namespace WukongApi.State
{
    public class MonsterState : CharacterState
    {
        public string Guid { get; }
        private readonly BUTamerActor? _pawn;

        public BUTamerActor? Pawn
        {
            get
            {
                if (_pawn.IsNullOrDestroyed())
                {
                    return null;
                }

                return _pawn;
            }
        }

        public float? Hp { get; set; }
        public bool IsSynced { get; set; }
        public bool IsTamerValid => !Pawn.IsNullOrDestroyed();

        public MonsterState(string guid, BUTamerActor pawn)
        {
            Guid = guid;
            _pawn = pawn;

            var monster = pawn.GetMonster();
            if (!monster.IsNullOrDestroyed())
            {
                TeamId = monster.GetTeamIDInCS();
            }
            else
            {
                Logging.LogError("Monster is null when creating monster state");
            }
        }

        public MonsterState(string guid, BUTamerActor pawn, int teamId)
        {
            Guid = guid;
            _pawn = pawn;
            TeamId = teamId;
        }
    }
}