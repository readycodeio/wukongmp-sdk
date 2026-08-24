using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the movie sequences that are currently playing or have finished playing in a given area.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MovieComponent(AllocatorKind allocatorKind)
{
    private NativeList<int> _startedSequences = new(8, allocatorKind);
    private NativeList<int> _finishedSequences = new(8, allocatorKind);
}
