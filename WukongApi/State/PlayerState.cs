using System.Collections.Concurrent;
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

        private APawn _pawn;

        public APawn Pawn
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

        private AActor _markerActor;

        public AActor MarkerActor
        {
            get
            {
                if (_markerActor != null && _markerActor.IsNullOrDestroyed())
                {
                    Logging.LogWarning("Marker actor is destroyed");
                    return null;
                }

                return _markerActor;
            }
            set => _markerActor = value;
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

        public float Hp { get; set; }
        public bool IsDead => Hp <= 0;
        public ConcurrentDictionary<EBGUAttrFloat, float> Attributes { get; }
        public EquipmentState Equipment { get; set; }
        public bool IsReadyForPvP { get; set; }
        public string NickName { get; set; }
        public bool RunImmobilizePatches { get; set; }
        public bool ReceivedPhantomRushExit { get; set; }
        public bool IsSpectator { get; set; }

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
            Attributes = new ConcurrentDictionary<EBGUAttrFloat, float>();

            Logging.LogDebug("Assigning team ID {TeamId} to player", teamId);
            PhotonUtils.RegisterNewPlayerTeam((BGUCharacterCS)pawn, teamId);
        }

        public void UpdateMarkerPosition()
        {
            if (MarkerActor != null)
            {
                var bguCharacterCs = Pawn as BGUCharacterCS;

                if (bguCharacterCs == null)
                {
                    Logging.LogError("Failed to cast pawn to BGUCharacterCS");
                    return;
                }

                var markerHeight = bguCharacterCs.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1;
                MarkerActor.SetActorLocation(Pawn.GetActorLocation() + new FVector(0, 0, markerHeight), false, out _, true);
            }
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