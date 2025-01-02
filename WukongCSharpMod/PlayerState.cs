using UnrealEngine.Engine;

namespace WukongCSharpMod
{
    public class PlayerState
    {
        public int PhotonId { get; }
        public APawn Pawn { get; set; }
        public bool LastIsFalling { get; set; }

        public PlayerState(int photonId, APawn pawn)
        {
            PhotonId = photonId;
            Pawn = pawn;
        }
    }
}