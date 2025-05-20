using System.Runtime.InteropServices;
using b1;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct LocalPlayerComponent
{
    public bool RunImmobilizePatches;
    public MontageState MontageState;
    
    public int TeleportFinishFrames;
    public bool ReceivedPhantomRushExit;
    
    private BGUCharacterCS? _pawn;

    public BGUCharacterCS? Pawn
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
}