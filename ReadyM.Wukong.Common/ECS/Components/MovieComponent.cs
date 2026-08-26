using System.Runtime.InteropServices;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the movie sequences that are currently playing or have finished playing in a given area.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MovieComponent : INativeInit
{
    private NativeList<int> _startedSequences;
    private NativeList<int> _finishedSequences;

    public void Init(AllocatorKind allocatorKind)
    {
        _startedSequences = new NativeList<int>(8, allocatorKind);
        _finishedSequences = new NativeList<int>(8, allocatorKind);
    }
}
