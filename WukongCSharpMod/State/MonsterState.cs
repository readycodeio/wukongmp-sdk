using System;
using b1;

namespace WukongCSharpMod.State
{
    public class MonsterState : CharacterState
    {
        public string Guid { get; }
        private readonly BUTamerActor _pawn;

        public BUTamerActor Pawn =>
            _pawn == null || _pawn.IsDestroyed ? throw new Exception("Attempting to access a destroyed Tamer") : _pawn;

        public float? Hp { get; set; }
        public bool IsSynced { get; set; }

        public MonsterState(string guid, BUTamerActor pawn)
        {
            Guid = guid;
            _pawn = pawn;

            if (pawn.GetMonster() != null)
            {
                TeamId = pawn.GetMonster().GetTeamIDInCS();
            }
            else
            {
                Helpers.LogError("Monster is null when creating monster state");
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