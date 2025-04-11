using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using b1;
using BtlShare;
using UnrealEngine.Runtime;

namespace WukongApi.State
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

        public PlayerState(int peerId, BGUCharacterCS pawn, int teamId, float initialHp, float initialHpMaxBase)
        {
            PeerId = peerId;
            Pawn = pawn;
            TeamId = teamId;
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

            Logging.LogDebug("Assigning team ID {TeamId} to player", teamId);
            PhotonUtils.RegisterNewPlayerTeam(pawn, teamId);
        }

        public override string ToString()
        {
            var realTeamId = Pawn?.GetTeamIDInCS();
            
            var sb = new StringBuilder("PlayerState");
            sb.AppendLine($"PeerId: {PeerId}");
            sb.AppendLine($"NickName: {NickName}");
            sb.AppendLine($"TeamID: {TeamId}");
            sb.AppendLine($"Real TeamId: {realTeamId}");
            sb.AppendLine($"Hp: {Hp}");
            sb.AppendLine($"Actual Hp: {BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(Pawn).GetFloatValue(EBGUAttrFloat.Hp)}");
            sb.AppendLine("------ ATTRIBUTES ------");
            sb.AppendLine(string.Join("\n", Attributes.Select(kvp => $"{kvp.Key}: {kvp.Value}").OrderBy(x => x)));
            sb.AppendLine("------ ANIMATION ------");
            sb.AppendLine($"InJump: {InJump}");
            sb.AppendLine($"IsFlying: {IsFlying}");
            sb.AppendLine($"IsFalling: {IsFalling}");
            sb.AppendLine($"IsLandingMove: {IsLandingMove}");
            sb.AppendLine($"Velocity: {Velocity}");
            sb.AppendLine($"MoveAcceleration: {MoveAcceleration}");
            sb.AppendLine($"ActorLocation: {Location}");
            sb.AppendLine($"ActorRotation: {Rotation}");
            sb.AppendLine($"TurnInplaceTargetRotation: {TurnInplaceTargetRotation}");
            sb.AppendLine($"IsStandRotate: {IsStandRotate}");
            sb.AppendLine($"TurnInplaceRemainAngle: {TurnInplaceRemainAngle}");
            sb.AppendLine($"IsAttacking: {IsAttacking}");
            sb.AppendLine($"OrientRotationToMovement: {OrientRotationToMovement}");
            sb.AppendLine($"MoveSpeedLevel: {MoveSpeedLevel}");
            sb.AppendLine($"MoveSpeedState: {MoveSpeedState}");
            sb.AppendLine($"ShouldWaitRotateFinished: {ShouldWaitRotateFinished}");

            return sb.ToString();
        }
    }
}