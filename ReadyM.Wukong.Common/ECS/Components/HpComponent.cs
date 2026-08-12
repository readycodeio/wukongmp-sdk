using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds entity HP, max HP, scaling factor and death state.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct HpComponent : IOwnershipBased
{
    private float _hp;
    private float _hpMaxBase;
    private int _hpMaxMulPercent;
    private bool _isDead;
}