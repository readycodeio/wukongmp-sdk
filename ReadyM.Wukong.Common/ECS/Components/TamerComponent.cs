using System.Runtime.InteropServices;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of a tamer (monster) entity. 
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TamerComponent : IOwnershipBased
{
    private string? _guid; // TODO: Unmanaged
    private string? _unitPath; // TODO: Unmanaged
    private NativeList<PlayerId> _holdingPlayers;
    private bool _hasFsmPaused;
    private bool _isBossOrElite;
    
    public bool ForceKeepSpawned => _holdingPlayers.Count > 0;
}