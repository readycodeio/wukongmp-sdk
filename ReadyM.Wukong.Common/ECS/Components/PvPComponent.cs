using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Wukong.Common.ECS.Values;

namespace ReadyM.Wukong.Common.ECS.Components;

// TODO: Register networked component
// TODO: Add to main character archetype
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PvPComponent
{
    private SpectatorReason _spectatorReason;
    private bool _isReadyForPvP;
    private bool _isSpectator;

    public bool IsObserver => _isSpectator && _spectatorReason == SpectatorReason.Observer;
}