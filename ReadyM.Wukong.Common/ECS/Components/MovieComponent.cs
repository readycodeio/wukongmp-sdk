using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MovieComponent
{
    private NativeList<int> _startedSequences;
    private NativeList<int> _finishedSequences;
}