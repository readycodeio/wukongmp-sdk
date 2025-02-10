using System.Collections.Generic;
using System.Text;
using b1;
using BtlShare;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi.State
{
    public class PlayerState : CharacterState
    {
        public int PhotonId { get; }
        public APawn Pawn { get; set; }

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

        public float Hp { get; set; }
        public bool IsDead => Hp <= 0;
        public Dictionary<EBGUAttrFloat, float> Attributes { get; }
        public EquipmentState Equipment { get; set; }
        public bool IsReadyForPvP { get; set; }
        public string NickName { get; set; }

        public PlayerState(int photonId, APawn pawn, int teamId)
        {
            PhotonId = photonId;
            Pawn = pawn;
            TeamId = teamId;

            // get the BUC_AttrContainer
            var data = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn);
            if (data != null)
            {
                Hp = data.GetFloatValue(EBGUAttrFloat.Hp);
            }
            else
            {
                Logging.LogError("Failed to get BUC_AttrContainer from pawn");
            }

            Equipment = EquipmentHelpers.GetCurrentEquipmentStateForActor(pawn);
            Attributes = new Dictionary<EBGUAttrFloat, float>();

            Logging.LogDebug($"Assigning team ID {teamId} to player");
            PhotonUtils.RegisterNewPlayerTeam((BGUCharacterCS)pawn, teamId);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("PlayerState");
            sb.AppendLine($"PhotonId: {PhotonId}");
            sb.AppendLine($"NickName: {NickName}");
            sb.AppendLine($"TeamID: {TeamId}");
            sb.AppendLine($"Hp: {Hp}");
            sb.AppendLine("------ ATTRIBUTES ------");

            foreach (var kvp in Attributes)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }

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