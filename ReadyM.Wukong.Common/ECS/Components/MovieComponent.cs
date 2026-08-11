using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the movie sequences that are currently playing or have finished playing in a given area.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MovieComponent
{
    private NativeList<int> _startedSequences;
    private NativeList<int> _finishedSequences;
}