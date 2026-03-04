using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Friflo.Json.Fliox;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MovieComponent
{
    private string? _startedSequencesEncoded;
    private string? _finishedSequencesEncoded;

    [Ignore]
    public ImmutableHashSet<int> StartedSequences
    {
        get
        {
            var str = StartedSequencesEncoded;
            return str == null ? [] : str.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToImmutableHashSet();
        }

        set => StartedSequencesEncoded = string.Join(";", value.Select(s => s.ToString()));
    }

    [Ignore]
    public ImmutableHashSet<int> FinishedSequences
    {
        get
        {
            var str = FinishedSequencesEncoded;
            return str == null ? [] : str.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToImmutableHashSet();
        }

        set => FinishedSequencesEncoded = string.Join(";", value.Select(s => s.ToString()));
    }
}
