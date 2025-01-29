using b1;
using BtlShare;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongCSharpMod.State
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
        public EquipmentState Equipment { get; set; }

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

            Logging.LogDebug($"Assigning team ID {teamId} to player");
            PhotonUtils.RegisterNewPlayerTeam((BGUCharacterCS)pawn, teamId);
        }

        public override string ToString()
        {
            return $"PlayerState(PhotonId: {PhotonId},\nInJump: {InJump},\nIsFlying: {IsFlying},\nIsFalling: {IsFalling},\nIsLandingMove: {IsLandingMove},\nVelocity: {Velocity},\nMoveAcceleration: {MoveAcceleration},\nActorLocation: {Location},\nActorRotation: {Rotation},\nTurnInplaceTargetRotation: {TurnInplaceTargetRotation},\nIsStandRotate: {IsStandRotate},\nTurnInplaceRemainAngle: {TurnInplaceRemainAngle},\nIsAttacking: {IsAttacking},\nOrientRotationToMovement: {OrientRotationToMovement},\nMoveSpeedLevel: {MoveSpeedLevel},\nMoveSpeedState: {MoveSpeedState},\nShouldWaitRotateFinished: {ShouldWaitRotateFinished},\nTeamID: {TeamId})\n";
        }
    }
}