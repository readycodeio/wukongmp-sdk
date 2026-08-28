using System.Runtime.InteropServices;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the movie sequences that have been started in a given area.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MovieComponent : INativeInit
{
    private NativeList<int> _startedSequences;

    public void Init(AllocatorKind allocatorKind)
    {
        _startedSequences = new NativeList<int>(8, allocatorKind);
    }
}
