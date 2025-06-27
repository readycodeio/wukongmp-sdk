using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using b1;
using BtlShare;
using ReadyM.Api.ECS.Idents;
using UnrealEngine.Runtime;
using WukongMp.Api.Old.Api;

namespace WukongMp.Api.Old.State
{
    public class PlayerState : CharacterState
    {
        private BGUCharacterCS? _pawn;

        public override BGUCharacterCS? Pawn
        {
            get
            {
                if (_pawn.IsNullOrDestroyed())
                {
                    Logging.LogWarning("Player pawn is null or destroyed");
                    return null;
                }

                return _pawn;
            }
            set => _pawn = value;
        }

        #region Animation

        public bool InJump { get; set; }
        public bool IsFlying { get; set; }
        public bool IsFalling { get; set; }
        public bool IsLandingMove { get; set; }
        public FRotator TurnInplaceTargetRotation { get; set; }
        public bool IsStandRotate { get; set; }
        public float TurnInplaceRemainAngle { get; set; }
        public bool IsAttacking { get; set; }
        public bool OrientRotationToMovement { get; set; }
        public bool ShouldWaitRotateFinished { get; set; }

        #endregion

        public ConcurrentDictionary<EBGUAttrFloat, float> Attributes { get; }
        public EquipmentState Equipment { get; set; }
        public bool IsReadyForPvP { get; set; }
        public bool ReceivedPhantomRushExit { get; set; }
        public bool IsSpectator { get; set; }
        public int TeleportFinishFrames { get; set; }
        public bool IsWaitingForSequence { get; set; }
        public bool IsJoiningSequence { get; set; }
        public FVector SequenceLocation { get; set; }
        public int WaitingSequenceId { get; set; }
        public float AIPathMoveStuckTimer { get; set; }
        public bool IsAIPathMoveStuck { get; set; }

        public PlayerState(PlayerId id, BGUCharacterCS pawn, int? teamId, float initialHp, float initialHpMaxBase)
        {
            PlayerId = id;
            _pawn = pawn;
            TeamId = teamId ?? pawn.GetTeamIDInCS();
            Hp = initialHp;
            Equipment = EquipmentHelpers.GetCurrentEquipmentStateForActor(pawn);
            Attributes = new ConcurrentDictionary<EBGUAttrFloat, float>();

            var attrContainer = (BUC_AttrContainer?)BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn);

            if (attrContainer != null)
            {
                var setHpMaxBase = attrContainer.SetFloatValue(EBGUAttrFloat.HpMaxBase, initialHpMaxBase);
                var setHp = attrContainer.SetFloatValue(EBGUAttrFloat.Hp, initialHp);
                Logging.LogDebug("Set actual Hp / HpMax: {Hp} {HpMax}", setHp, setHpMaxBase);
            }
            else
            {
                Logging.LogError("Failed to get attribute container from player");
            }

            if (teamId != null)
            {
                Logging.LogDebug("Assigning team ID {TeamId} to player", teamId.Value);
                ClientUtils.RegisterNewPlayerTeam(pawn, teamId.Value);
            }
        }

        public override string ToString()
        {
            var realTeamId = Pawn?.GetTeamIDInCS();

            List<string> lines =
            [
                $"Real TeamId: {realTeamId}",
                $"Actual Hp: {BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(Pawn).GetFloatValue(EBGUAttrFloat.Hp)}",
            ];

            lines.AddRange(Attributes.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

            // reflection - print every public property
            var properties = GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.Name is nameof(Pawn) or nameof(Attributes) or nameof(Equipment))
                    continue;

                var value = property.GetValue(this);
                if (value is IEnumerable<int> enumerable)
                {
                    lines.Add($"{property.Name}: {string.Join(", ", enumerable)}");
                }
                else
                {
                    lines.Add($"{property.Name}: {value}");
                }
            }

            lines.Sort();

            var sb = new StringBuilder();

            sb.AppendLine("-------------------------");
            sb.AppendLine("PLAYER STATE:");

            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}