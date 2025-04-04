using b1;
using BtlShare;
using UnrealEngine.Runtime;

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

        public bool IsSynced { get; set; }
        public bool IsTamerValid => !Tamer.IsNullOrDestroyed();

        public MonsterState(string guid, BUTamerActor tamer, string unitName)
        {
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

        public MonsterState(string guid, BUTamerActor tamer, int teamId, string unitName)
        {
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

        public override void UpdateMarkerPosition()
        {
            if (MarkerActor != null)
            {
                var bguCharacterCs = Tamer?.GetMonster() as BGUCharacterCS;

                if (bguCharacterCs == null)
                {
                    Logging.LogError("Failed to cast monster pawn to BGUCharacterCS");
                    return;
                }

                var markerHeight = bguCharacterCs.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1;
                MarkerActor.SetActorLocation(bguCharacterCs.GetActorLocation() + new FVector(0, 0, markerHeight), false, out _, true);
            }
        }
    }
}