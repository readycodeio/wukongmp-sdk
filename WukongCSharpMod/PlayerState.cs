using System.Numerics;
using UnrealEngine.Engine;

namespace WukongCSharpMod
{
    public class PlayerState
    {
        public int PhotonId { get; }
        public Vector2 LastMovement { get; set; } = Vector2.Zero;
        public APawn Pawn { get; }

        public PlayerState(int photonId, APawn pawn)
        {
            PhotonId = photonId;
            Pawn = pawn;
        }
    }
}