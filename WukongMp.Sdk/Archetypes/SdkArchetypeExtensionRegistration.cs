using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.SDK.Client.Archetypes;
using ReadyM.SDK.Core;

namespace WukongMp.Sdk.Archetypes;

/// Adds what the loaded mods' shapes declared to the archetypes the game registers. Registered
/// after the archetype registrations it reads ids from, since registrations run in order.
internal sealed class SdkArchetypeExtensionRegistration(
    DefaultWorldArchetypeRegistration world,
    DefaultAreaArchetypeRegistration area,
    DefaultPlayerArchetypeRegistration player,
    DefaultCellArchetypeRegistration cell,
    IEnumerable<IArchetypeShapeBindings> bindings,
    ILoggerFactory loggerFactory
) : IArchetypeRegistration
{
    public void Register(IArchetypeRegistry registry)
    {
        var bound = Bound();
        var applied = ClientArchetypes.ApplyExtensions(
            registry,
            shape => bound.TryGetValue(shape.FullName!, out var id) ? id : null);

        loggerFactory
            .CreateLogger<SdkArchetypeExtensionRegistration>()
            .LogDebug("Added {Count} component(s) from mod shapes to the game's archetypes", applied);
    }

    /// Which archetype each shape is. The four every game has come from the SDK's own
    /// registrations; the rest are whatever this game said its archetypes are called.
    private Dictionary<string, ArchetypeId> Bound()
    {
        var bound = new Dictionary<string, ArchetypeId>
        {
            [typeof(World).FullName!] = world.WorldArchetype,
            [typeof(Area).FullName!] = area.AreaArchetype,
            [typeof(Player).FullName!] = player.PlayerArchetype,
            [typeof(Cell).FullName!] = cell.CellArchetype
        };

        foreach (var binding in bindings)
            foreach (var (shape, archetype) in binding.Bindings)
                bound[shape] = archetype;

        return bound;
    }
}
