using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Friflo.Json.Fliox;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TamerComponent : IOwnershipManaged
{
    private string? _guid;
    private string? _unitPath;
    private string? _holdingPlayersEncoded;
    private bool _hasFsmPaused;

    [Ignore]
    public ImmutableHashSet<PlayerId> HoldingPlayers
    {
        get
        {
            var str = HoldingPlayersEncoded;
            return str == null ? [] : str.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(s => new PlayerId(ushort.Parse(s))).ToImmutableHashSet();
        }

        set => HoldingPlayersEncoded = string.Join(";", value.Select(s => s.RawValue.ToString()));
    }

    public bool ForceKeepSpawned => HoldingPlayers.Count > 0;
}