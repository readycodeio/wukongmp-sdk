using b1;
using System;

namespace WukongApi.State
{
    public class MonsterState : CharacterState
    {
        public string Guid { get; }
        public string UnitName { get; }

        private readonly BUTamerActor? _tamer;

        public BUTamerActor? Tamer
        {
            get
            {
                if (_tamer.IsNullOrDestroyed())
                {
                    return null;
                }

                return _tamer;
            }
        }

        public override BGUCharacterCS? Pawn
        {
            get
            {
                if (_tamer == null || _tamer.IsNullOrDestroyed() || _tamer.GetMonster().IsNullOrDestroyed())
                {
                    Logging.LogWarning("Tamer or monster is null or destroyed");
                    return null;
                }

                return _tamer.GetMonster();
            }
            set => throw new NotSupportedException("Set monster pawn");
        }

        public bool IsSynced { get; set; }
        public bool IsTamerValid => !Tamer.IsNullOrDestroyed();
        public EBGUMoveAIType MoveAIType { get; set; }

        public MonsterState(int id, string guid, BUTamerActor tamer, string unitName)
        {
            PeerId = id;
            Guid = guid;
            _tamer = tamer;
            UnitName = unitName;

            var monster = tamer.GetMonster();
            if (!monster.IsNullOrDestroyed())
            {
                TeamId = monster.GetTeamIDInCS();
            }
            else
            {
                Logging.LogError("Monster is null when creating monster state");
            }

            Logging.LogDebug("Created monster state with team ID: {TeamId}", TeamId);
        }

        public MonsterState(int id, string guid, BUTamerActor tamer, int teamId, string unitName)
        {
            PeerId = id;
            Guid = guid;
            _tamer = tamer;
            TeamId = teamId;
            UnitName = unitName;

            Logging.LogDebug("Created monster state with team ID: {TeamId} (assigned)", TeamId);
        }

        public override string ToString()
        {
            var realTeamId = Tamer?.GetMonster().GetTeamIDInCS();
            return $"MonsterState: Guid={Guid}, TeamId={TeamId}, RealTeamId={realTeamId} Hp={Hp}, IsSynced={IsSynced}, IsTamerValid={IsTamerValid}";
        }
    }
}