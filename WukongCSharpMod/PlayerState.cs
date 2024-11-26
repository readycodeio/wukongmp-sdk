using b1;
using UnrealEngine.Engine;

namespace WukongCSharpMod
{
    public class PlayerState
    {
        public int PhotonId { get; }
        public EAIMoveSpeedType MovementType { get; set; } = EAIMoveSpeedType.RUN;
        public ESkillDirection LastMovement { get; set; } = ESkillDirection.None;
        public APawn Pawn { get; }

        public PlayerState(int photonId, APawn pawn)
        {
            PhotonId = photonId;
            Pawn = pawn;
        }
    }
}