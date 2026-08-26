using System.Runtime.InteropServices;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of a tamer (monster) entity.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TamerComponent : IOwnershipBased, INativeInit
{
    private NativeString256 _guid;
    private NativeString256 _unitPath;
    private NativeList<PlayerId> _holdingPlayers;
    private bool _hasFsmPaused;
    private bool _isBossOrElite;

    public bool ForceKeepSpawned => _holdingPlayers.Count > 0;

    public void Init(AllocatorKind allocatorKind)
    {
        _holdingPlayers = new NativeList<PlayerId>(8, allocatorKind);
    }
}
